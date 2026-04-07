# TypeScript MCP SDK Reference

> **Load when:** Building an MCP server or client with Node.js/TypeScript.

## Server Implementation

### Setup

```bash
npm init -y
npm install @modelcontextprotocol/sdk zod
npm install -D typescript @types/node
```

### Minimal MCP Server (stdio)

```typescript
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { z } from "zod";

const server = new McpServer({
  name: "order-tools",
  version: "1.0.0",
});

// Register a tool
server.tool(
  "create-order",
  "Creates a new order transaction between buyer and seller",
  {
    buyerId: z.string().describe("Buyer's account ID"),
    sellerId: z.string().describe("Seller's account ID"),
    amountCents: z.number().int().positive().describe("Escrow amount in cents"),
    currency: z.string().length(3).default("USD").describe("ISO 4217 currency code"),
  },
  async ({ buyerId, sellerId, amountCents, currency }) => {
    // Call your backend API
    const response = await fetch("https://api.example.com/orders", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ buyerId, sellerId, amountCents, currency }),
    });

    if (!response.ok) {
      return {
        content: [{ type: "text", text: `Error: ${response.statusText}` }],
        isError: true,
      };
    }

    const order = await response.json();
    return {
      content: [{
        type: "text",
        text: `Escrow ${order.id} created: ${(amountCents / 100).toFixed(2)} ${currency}`,
      }],
    };
  }
);

// Register a resource
server.resource(
  "order",
  "order://{orderId}",
  async (uri) => {
    const orderId = uri.pathname.split("/").pop();
    const response = await fetch(`https://api.example.com/orders/${orderId}`);
    const order = await response.json();

    return {
      contents: [{
        uri: uri.href,
        mimeType: "application/json",
        text: JSON.stringify(order, null, 2),
      }],
    };
  }
);

// Register a prompt
server.prompt(
  "analyze-order",
  "Analyzes an order transaction for risk factors",
  { orderId: z.string().describe("The order ID to analyze") },
  async ({ orderId }) => {
    const response = await fetch(`https://api.example.com/orders/${orderId}`);
    const order = await response.json();

    return {
      messages: [
        {
          role: "user",
          content: {
            type: "text",
            text: `Analyze this order transaction for risk factors:\n\n${JSON.stringify(order, null, 2)}\n\nConsider: amount thresholds, party verification status, geographic risk, and transaction velocity.`,
          },
        },
      ],
    };
  }
);

// Start the server
const transport = new StdioServerTransport();
await server.connect(transport);
```

### HTTP+SSE Transport

```typescript
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { SSEServerTransport } from "@modelcontextprotocol/sdk/server/sse.js";
import express from "express";

const app = express();
const server = new McpServer({ name: "order-tools", version: "1.0.0" });

// Register tools, resources, prompts...

let transport: SSEServerTransport;

app.get("/sse", async (req, res) => {
  transport = new SSEServerTransport("/message", res);
  await server.connect(transport);
});

app.post("/message", async (req, res) => {
  await transport.handlePostMessage(req, res);
});

app.listen(3001, () => console.log("MCP server on port 3001"));
```

## Client Implementation

### Connecting to an MCP Server

```typescript
import { Client } from "@modelcontextprotocol/sdk/client/index.js";
import { StdioClientTransport } from "@modelcontextprotocol/sdk/client/stdio.js";

const transport = new StdioClientTransport({
  command: "dotnet",
  args: ["run", "--project", "src/MyApp.McpServer"],
});

const client = new Client({ name: "my-app", version: "1.0.0" });
await client.connect(transport);

// List available tools
const tools = await client.listTools();
console.log("Available tools:", tools.tools.map(t => t.name));

// Call a tool
const result = await client.callTool("create-order", {
  buyerId: "USR-001",
  sellerId: "USR-002",
  amountCents: 500000,
  currency: "USD",
});
console.log("Result:", result.content);

// Read a resource
const resource = await client.readResource("order://ESC-12345");
console.log("Escrow:", resource.contents[0].text);

// Disconnect
await client.close();
```

## Error Handling

```typescript
server.tool(
  "process-payment",
  "Processes a payment for an order",
  {
    orderId: z.string(),
    amount: z.number().positive(),
  },
  async ({ orderId, amount }) => {
    try {
      const result = await processPayment(orderId, amount);
      return {
        content: [{ type: "text", text: `Payment processed: ${result.transactionId}` }],
      };
    } catch (error) {
      // Return error as tool result (not JSON-RPC error)
      return {
        content: [{ type: "text", text: `Payment failed: ${error.message}` }],
        isError: true,
      };
    }
  }
);
```

## Client Configuration

### For Copilot CLI (mcp-config.json)

```json
{
  "mcpServers": {
    "order-tools-ts": {
      "command": "node",
      "args": ["dist/server.js"],
      "cwd": "/path/to/mcp-server",
      "env": {
        "API_BASE_URL": "https://api.example.com",
        "NODE_ENV": "production"
      }
    }
  }
}
```

### For Claude Desktop (claude_desktop_config.json)

```json
{
  "mcpServers": {
    "order-tools": {
      "command": "npx",
      "args": ["-y", "@myapp/order-mcp-server"],
      "env": {
        "API_KEY": "your-api-key"
      }
    }
  }
}
```

## Best Practices

1. **Validate all inputs** — Use Zod schemas; never trust AI-provided input
2. **Return structured errors** — Use `isError: true` in tool results for expected failures
3. **Keep tools focused** — One tool per operation; don't create "do-everything" tools
4. **Add descriptions** — Every tool, parameter, and resource needs clear documentation
5. **Handle timeouts** — AI clients may cancel long-running operations
6. **Log to stderr** — stdout is the protocol channel; use stderr for debugging
