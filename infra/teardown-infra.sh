# 1. Delete just the OpenAI resource directly
az cognitiveservices account delete \
  --name cognidoc-dev-openai3 \
  --resource-group rg-cognidoc-dev

# 2. Purge it to release your 350k TPM quota right away
az cognitiveservices account purge \
  --name cognidoc-dev-openai3 \
  --resource-group rg-cognidoc-dev \
  --location australiaeast

# 3. Fire-and-forget delete on the rest of the resource group
az group delete --name rg-cognidoc-dev --yes --no-wait