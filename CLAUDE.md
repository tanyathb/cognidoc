# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

CogniDoc is an event-driven document ingestion pipeline on Azure. You upload a document through the Web API, it lands in Blob Storage, and an Azure Functions worker processes it: chunk, embed, index in AI Search, summarise, then store metadata in Cosmos DB. Ingestion works. Retrieval and chat (RAG over the index) are still in progress; `IRagSearchService` is the placeholder for that work.

## Commands

Prerequisites: .NET 10 SDK, Azure Functions Core Tools, Node 20+, Azure CLI, and Azurite for local Blob Storage.

```bash
# Backend (solution is backend/CogniDocEngine.sln)
cd backend && dotnet build
dotnet run --project backend/CogniDoc.WebAPI          # Web API
cd backend/CogniDoc.Functions && func start           # Functions host (needs local.settings.json)

# Frontend (Vite dev server on port 50077)
cd frontend/cognidoc.client && npm install && npm run dev
npm run lint
npm run build

# Infrastructure (resource-group scoped Bicep)
az deployment group create --resource-group rg-cognidoc-dev \
  --template-file infra/main.bicep \
  --parameters developerPrincipalId=$(az ad signed-in-user show --query id -o tsv)
```

There are no automated tests yet.

`infra/teardown-infra.sh` deletes and purges the OpenAI account, which releases its TPM quota, and then deletes the whole resource group. Don't run it unless asked.

## Architecture

The flow runs in this order:

1. The browser posts to `POST /api/upload` (WebAPI `UploadController`).
2. The Web API writes the file to the `documents` container as `{guid}-{filename}`.
3. `BlobIngestionFunction` (a BlobTrigger) builds a `DocumentProcessingMessage` and enqueues it on the Service Bus queue.
4. `DocumentProcessorFunction` (a ServiceBusTrigger) runs these steps:
   1. Validate the message.
   2. Fetch the blob.
   3. Chunk the text.
   4. Embed the chunks.
   5. Index them in AI Search.
   6. Summarise the document.
   7. Upsert metadata to Cosmos DB.
   8. Complete the message.

Invariants to keep when you change the pipeline:

- **Upload and processing stay decoupled.** The API only puts bytes into Blob Storage. All AI work happens asynchronously in Functions.
- **Idempotency:** the ingestion function sets the Service Bus `MessageId` to the `JobId` so that queue deduplication absorbs duplicate blob triggers.
- **Message settlement is manual.** `host.json` sets `autoCompleteMessages: false` and `maxConcurrentCalls: 1`, so the processor must explicitly complete or dead-letter each message.
  - Dead-letter unrecoverable input with a reason (`CorruptPayload`, `BlobNotFound`).
  - Rethrow unexpected exceptions so the Functions runtime handles retries and the poison queue. Don't swallow them.
  - Complete the message only after every step has succeeded.
- **Chunking** is by character count: 1200 characters with 150 characters of overlap. Embeddings go to Azure OpenAI in sub-batches of 6, wrapped in a Polly v8 resilience pipeline (`Microsoft.Extensions.Resilience`), with a delay between batches to avoid burst RPM throttling. Summarisation uses the same pipeline.
- **Text extraction** reads blobs as UTF-8. PDF and DOCX are recognised by content type but not parsed.

### Backend projects

- `CogniDoc.WebAPI`: ASP.NET Core controllers for upload and health. In Development it uses the `AzureStorage` connection string, falling back to Azurite. In other environments it uses `Storage:BlobEndpoint` with `DefaultAzureCredential`. The CORS policy allows `localhost` and `*.azurestaticapps.net`. There is no auth on the API.
- `CogniDoc.Functions`: Azure Functions, isolated worker model. It references `CogniDoc.Application`. DI is composed in `Program.cs` through two extension methods:
  - `AddInfrastructureServices` chains the per-service registrations in `Extensions/Infrastructure/`: identity, Cosmos, Search, OpenAI with resilience, and storage plus messaging.
  - `AddApplicationServices` registers `DocumentAiService`.
- `CogniDoc.Application`: shared models (`DocumentEntity`, `ProcessingTask`).
- `CogniDoc.Infrastructure`: a standalone console host that provisions the AI Search index (`AzureSearchIndexService`). It reads `SearchService:serviceUri`.
- `CogniDoc.Domain`: currently empty.

### Functions configuration binding

Triggers bind to configuration keys through constants in `CogniDoc.Functions/Configuration/`. If you rename a setting, update the constant and the app settings in `infra/modules/functionapp.bicep` together.

| Setting | Configuration key | Defined in |
|---|---|---|
| Blob trigger path | `%Storage:ContainerName%/{name}` | `StorageOptions` |
| Blob connection name | `Storage:BlobConnection` | `StorageOptions` |
| Queue name | `%Queues:DocumentProcessingQueueName%` | `ServiceBusOptions` |
| Service Bus connection name | `ServiceBusConnection` | `ServiceBusOptions` |
| OpenAI settings | `AzureOpenAI` section | `AzureOpenAIOptions` |

`AzureOpenAIOptions` sets default deployment names: `gpt-5-mini` for chat and `text-embedding-3-large` for embeddings.

### Identity and security

There are no connection strings or keys in the code or the deployed config.

- Every Azure client uses `DefaultAzureCredential`.
- The Web API and the Function App each have a system-assigned managed identity. `infra/modules/rbac.bicep` grants each one only the built-in roles it needs. Role definition GUIDs there have been corrected before, so verify any GUID against Azure's built-in role list before you change it.
- `AzureOpenAIOptions.ApiKey` is an optional escape hatch for local development only.
- Local debugging needs two things: `az login`, and your own principal ID passed as `developerPrincipalId` when you deploy, so the RBAC module grants you access.
- `local.settings.json` and `.env.local` are gitignored.

### Frontend

The frontend is React 19 with Vite in `frontend/cognidoc.client`. JSX and TSX files are mixed. `FileUpload.tsx` posts multipart data to `${VITE_API_URL}/api/upload`, defaulting to `https://localhost:7001`.

A commented-out alternative in `FileUpload.tsx` uploads directly to Blob Storage with a SAS URL through `/api/upload/initiate`. The `UploadInitiateRequest` and `UploadInitiateResponse` models exist, but the endpoint does not.

### Infra and CI/CD

- `infra/main.bicep` composes one module per resource: storage, cosmos, search, openai, servicebus, staticwebapp, appservice, functionapp, and rbac.
- Resource names and regions are in `infra/config.json`. The main region is `australiaeast`; OpenAI is deployed to `eastus`.
- Compiled ARM `*.json` files sit next to some modules.
- The four workflows in `.github/workflows/` deploy on push to `main`, filtered by path:
  - `infra/**` deploys infrastructure.
  - `backend/**` deploys the Web API.
  - `backend/CogniDoc.Functions/**` deploys the Functions app.
  - `frontend/**` deploys the Static Web App.
- The workflows authenticate to Azure with OIDC.
