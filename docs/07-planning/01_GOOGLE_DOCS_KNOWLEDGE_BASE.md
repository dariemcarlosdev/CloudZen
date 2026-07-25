# Plan Implementation: Google Docs Knowledge Base for Chatbot

**Date:** 2026-05-31  
**Feature:** Replace hardcoded knowledge base in `ChatFunction.cs` with Google Docs  
**Status:** Pending  
**Priority:** High

---

## Objective

Replace the hardcoded `KnowledgeBase` constant in `Api/Features/Chat/ChatFunction.cs` with content dynamically fetched from a Google Docs document. This enables non-developers to update the chatbot's knowledge base without modifying code.

---

## Scope

| Include | Exclude |
|---------|---------|
| Google Docs API integration | OAuth user flow (use Service Account) |
| Caching mechanism | Multiple documents (single doc only) |
| Fallback to hardcoded content | Real-time document syncing |
| Configuration via settings | Google Drive API access |

---

## Implementation Checklist

### Phase 1: Google Cloud Setup
- [ ] **1.1** Create Google Cloud project (if not exists)
- [ ] **1.2** Enable Google Docs API
- [ ] **1.3** Enable Google Drive API (for document access)
- [ ] **1.4** Create Service Account
- [ ] **1.5** Generate Service Account JSON key
- [ ] **1.6** Share the Google Doc with the Service Account email
- [ ] **1.7** Copy the Document ID from the Google Doc URL

### Phase 2: Project Configuration
- [ ] **2.1** Add `Google.Apis.Docs.v1` NuGet package to `CloudZen.Api.csproj`
- [ ] **2.2** Add `Google.Apis.Auth` NuGet package to `CloudZen.Api.csproj`
- [ ] **2.3** Add configuration keys to `local.settings.json`:
  - `GOOGLE_DOC_ID`
  - `GOOGLE_SERVICE_ACCOUNT_JSON` (or path to file)
- [ ] **2.4** Add configuration keys to Azure App Settings (for deployment)

### Phase 3: Service Implementation
- [ ] **3.1** Create `GoogleDocsService.cs` in `Api/Shared/Services/`
- [ ] **3.2** Implement `IGoogleDocsService` interface
- [ ] **3.3** Implement document fetching with content parsing
- [ ] **3.4** Implement in-memory caching with configurable TTL
- [ ] **3.5** Add logging for fetch success/failure
- [ ] **3.6** Implement fallback to hardcoded content on failure

### Phase 4: ChatFunction Integration
- [ ] **4.1** Inject `IGoogleDocsService` into `ChatFunction`
- [ ] **4.2** Replace hardcoded `KnowledgeBase` with service call
- [ ] **4.3** Add cache refresh logic (e.g., on startup or TTL-based)
- [ ] **4.4** Handle service failures gracefully

### Phase 5: Testing
- [ ] **5.1** Test local with valid Google Doc
- [ ] **5.2** Test fallback when document is inaccessible
- [ ] **5.3** Test cache invalidation
- [ ] **5.4** Test Azure deployment with App Settings

### Phase 6: Documentation
- [ ] **6.1** Update `docs/03-features/03_FEATURE_CHATBOT.md`
- [ ] **6.2** Document how to update the knowledge base
- [ ] **6.3** Document Service Account setup process

---

## Technical Details

### New Files
```
Api/
└── Shared/
    └── Services/
        └── GoogleDocsService.cs    (new)
```

### Modified Files
```
Api/Features/Chat/
└── ChatFunction.cs                 (inject service, replace KnowledgeBase)
Api/Shared/
└── CloudZen.Api.csproj             (add NuGet packages)
```

### Configuration Keys
| Key | Description | Required |
|-----|-------------|----------|
| `GOOGLE_DOC_ID` | Google Docs document ID | Yes |
| `GOOGLE_SERVICE_ACCOUNT_JSON` | Base64-encoded or raw JSON credentials | Yes |
| `KNOWLEDGE_BASE_CACHE_TTL_MINUTES` | Cache TTL (default: 60) | No |

### Google Doc Format
The document should contain the knowledge base content in plain text or markdown format. The service will fetch the entire document body.

---

## Alternative Approaches Considered

| Approach | Pros | Cons |
|----------|------|------|
| **Google Docs (Selected)** | Easy for non-devs to edit, version history, collaborative | Requires Google Cloud setup |
| **Azure Blob Storage** | Simple, Azure-native | Less user-friendly for editing |
| **Database table** | Full control, structured | Requires database access, less portable |
| **CMS integration** | Rich editing experience | Overkill for this use case |

---

## Rollback Plan

If the Google Docs integration fails:
1. The service returns the original hardcoded `KnowledgeBase`
2. Logs indicate "Using fallback knowledge base"
3. No user-facing errors occur

---

## Success Criteria

- [ ] Chatbot responds using content from Google Docs
- [ ] Updates to Google Doc reflected within cache TTL
- [ ] No user-facing errors when Google Docs is unavailable
- [ ] Non-developer can update knowledge base without code changes
- [ ] Build passes (`dotnet build`)
- [ ] Tests pass (`dotnet test`)
