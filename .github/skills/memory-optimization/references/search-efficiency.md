# Search Efficiency — Deep Dive

## Progressive Disclosure Pattern

1. **Find files** — `glob` or `grep` with `files_with_matches`
2. **Count matches** — `grep` with `count` to assess scope
3. **Read specific matches** — `grep` with `content` and `-n` on targeted files
4. **Deep dive** — `view` with `view_range` on the most relevant result

## Grep/Glob Best Practices

```
✅ grep pattern:"IFundHoldable" glob:"**/*.cs" output_mode:"files_with_matches"
   → Fast: returns only file paths

❌ grep pattern:"IFundHoldable" output_mode:"content" -A:50
   → Wasteful: loads 50 lines of context per match across entire repo
```

## Tool Selection Priority

1. **Code Intelligence Tools** (if available) — semantic search, symbol lookup
2. **LSP** (if available) — goToDefinition, findReferences, hover
3. **glob** — find files by name pattern
4. **grep with glob filter** — find content within specific file types
5. **powershell** — last resort for complex searches

## Scoping Rules

Always narrow search scope:

| Change Area | Search Directory |
|-------------|-----------------|
| UI changes | `Components/`, `Pages/`, `Layout/` |
| Business logic | `Features/` |
| Data access | `Data/` |
| Payment flow | `Services/Strategies/` |
| Domain model | `Models/`, `Events/` |
| Configuration | `Program.cs`, `appsettings*.json` |
