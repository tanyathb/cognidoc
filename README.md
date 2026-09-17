# CogniDoc

An asynchronous, event-driven document processing and retrieval pipeline on Azure. Documents are uploaded through a web API, land in Blob Storage, and are processed out-of-band by an Azure Functions worker that chunks the text, generates embeddings, indexes them for vector search, and writes metadata to Cosmos DB.

This is a personal learning project, in active development. I started it while preparing for Microsoft AI-200, to work through event-driven patterns, managed-identity authentication and infrastructure-as-code end to end rather than in isolated tutorials. The ingestion pipeline is working; the retrieval and chat side is in progress. See [Status](#status).

---

## Why it is built this way

Embedding generation is slow and rate-limited. Doing it inside the upload request would mean holding an HTTP connection open for the length of an LLM call, and failing the user's upload whenever Azure OpenAI throttles.

So the upload path and the processing path are separated by a queue. The API's only job is to get bytes into Blob Storage and return. Everything after that happens on its own schedule, can retry, and can fail without the user noticing.

```
  Browser (React + Vite)
        │  POST /api/upload
        ▼
  Web API (ASP.NET Core, App Service)
        │  writes blob
        ▼
  Blob Storage ──── BlobTrigger ────► Ingestion Function
                                            │  enqueue job
                                            ▼
                                      Service Bus queue
                                            │
                                            ▼
                                   Document Processor Function
                                            │
                    ┌───────────────────────┼───────────────────────┐
                    ▼                       ▼                       ▼
            Azure OpenAI            Azure AI Search             Cosmos DB
         (embeddings + summary)    (vector index, HNSW)     (metadata + audit)
```

## How processing works

The processor function handles one queue message at a time and walks through a fixed sequence:

1. **Deserialize and validate.** A malformed payload is dead-lettered immediately with reason `CorruptPayload` rather than retried — a message that failed to parse will never parse.
2. **Fetch the blob.** A 404 is dead-lettered as `BlobNotFound`. An empty document completes without indexing; there is nothing to embed.
3. **Chunk with overlap.** 1200 characters with 150 characters of overlap, so a sentence spanning a boundary still appears whole in one chunk.
4. **Embed in sub-batches.** Chunks go to Azure OpenAI six at a time, wrapped in a Polly resilience pipeline, with a short delay between batches to stay under burst RPM limits.
5. **Index.** Chunks and their vectors are uploaded to Azure AI Search as a batch.
6. **Summarise.** A chat deployment produces a document summary through the same resilience pipeline.
7. **Persist.** Metadata and the summary are upserted into Cosmos DB, partitioned by job ID.
8. **Complete.** The message is settled only after every step above has succeeded.

Two details worth calling out. The ingestion function sets `MessageId` to the job ID, so Service Bus deduplicates if the blob trigger fires twice. And unexpected exceptions are rethrown rather than swallowed, leaving retry and poison-queue handling to the Functions runtime instead of reimplementing it.

## Security

There are no connection strings or API keys anywhere in the codebase or the deployed configuration.

Every service is reached through `DefaultAzureCredential`. The Function App and the Web API each have their own system-assigned managed identity, and the Bicep RBAC module grants each one only the built-in roles it needs — Storage Blob Data Owner, Cognitive Services OpenAI User, Search Index Data Contributor, Service Bus Data Owner, and the Cosmos DB SQL data-plane contributor role. A developer principal ID can be passed in to grant the same access for local debugging, and is optional so deployments do not require it.

GitHub Actions authenticates to Azure with OIDC federated credentials rather than a stored service principal secret.

## Stack

| Layer | Technology |
|---|---|
| Frontend | React, Vite, TypeScript — Azure Static Web Apps |
| API | ASP.NET Core Web API (.NET 10) — Azure App Service |
| Processing | Azure Functions, isolated worker model (.NET 10) |
| Messaging | Azure Service Bus |
| Storage | Azure Blob Storage |
| Vectors | Azure AI Search (HNSW) |
| Metadata | Azure Cosmos DB |
| AI | Azure OpenAI — embeddings and chat |
| Resilience | Polly v8 |
| Infrastructure | Bicep, modular |
| CI/CD | GitHub Actions with OIDC |

## Layout

```
backend/
  CogniDoc.WebAPI/          Upload and health endpoints
  CogniDoc.Functions/       Blob ingestion and document processor
  CogniDoc.Application/     Shared models
  CogniDoc.Infrastructure/  Search index provisioning
frontend/cognidoc.client/   React upload UI
infra/                      Bicep modules, one per Azure resource
.github/workflows/          Four deployment pipelines
```

## Running it

Prerequisites: .NET 10 SDK, Azure Functions Core Tools, Node 20+, Azure CLI, and an Azure subscription with access to Azure OpenAI.

```bash
# Provision infrastructure (main.bicep targets a resource group)
az login
az group create --name rg-cognidoc-dev --location australiaeast
az deployment group create \
  --resource-group rg-cognidoc-dev \
  --template-file infra/main.bicep \
  --parameters developerPrincipalId=$(az ad signed-in-user show --query id -o tsv)

# Backend
cd backend && dotnet build

# Frontend
cd frontend/cognidoc.client && npm install && npm run dev
```

Local development uses Azurite for Blob Storage. `local.settings.json` and `.env.local` are gitignored; create them from your own resource names. Sign in with `az login` so `DefaultAzureCredential` can pick up your identity, and pass your own principal ID as `developerPrincipalId` when deploying so the RBAC assignments include you.

## Status

Ingestion works end to end: upload, queue, chunk, embed, index, summarise, persist.

The retrieval half is next. The index is populated and queryable, but nothing queries it yet. `IRagSearchService` defines that contract — `SearchRelevantChunksAsync` is the missing piece. The plan is to embed the user's question with the same model used at ingestion, run a vector search against the index, and pass the closest chunks to a chat deployment as grounding context, so answers are drawn from the uploaded documents rather than the model's own training data. A chat endpoint on the Web API and a chat view in the React client follow from that.

Other things still open:

- No automated tests. The chunking function is pure and would be the obvious place to start.
- Text extraction reads blobs as UTF-8 directly, so PDF and DOCX are recognised by content type but not actually parsed.
- No authentication on the API. The upload endpoint is open.
- Chunking splits on character count rather than sentence or paragraph boundaries, which will affect retrieval quality once the query path exists.
