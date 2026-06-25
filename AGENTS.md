# AGENTS.md

> Permanent instruction set for any AI coding agent (Claude Code, GPT, Copilot, etc.) working on this repository.

## 1. Project Overview

This is a **learning project** for an AI Engineering course, focused on building practical experience with **Microsoft Agent Framework (MAF)**.

The goals of this project are to demonstrate and practice:

- **AI Agents** — building focused, single-purpose agents powered by LLMs
- **Tool Calling** — exposing typed functions to agents and handling tool execution loops
- **Structured Output** — getting strongly-typed, schema-validated responses from models instead of free text
- **Multi-Agent Systems** — composing multiple specialized agents (sequential, concurrent, group chat, handoff patterns)
- **MAF Workflows** — building explicit, auditable graphs of executors and edges that mix deterministic code with agentic decision-making

This is an educational codebase. Code should be correct and well-structured, but the priority is **clarity and demonstrating concepts correctly** over production scale.

---

## 2. Technical Stack

- **Language:** C#
- **Runtime:** .NET (latest LTS)
- **Framework:** Microsoft Agent Framework (MAF) — `agent_framework` packages
- **Model provider:** Azure OpenAI–compatible models (via `AzureOpenAIChatClient` or compatible clients)
- **Version control:** Git / GitHub

Do not introduce additional frameworks (e.g., LangChain, Semantic Kernel directly, AutoGen) unless explicitly requested — MAF is the chosen abstraction layer for this project.

---

## 3. Architecture Principles

- Follow **clean architecture** with clear separation of concerns:
  - **Domain/Business logic** — plain C# classes, no agent or LLM dependencies
  - **Agent layer** — agent definitions, instructions, tool registration
  - **Workflow layer** — executors, edges, routing logic
  - **Infrastructure** — model clients, configuration, persistence
- **Business logic must not depend on agent/LLM code.** Agents call into business logic via tools or services — never the reverse.
- Use **dependency injection** (`Microsoft.Extensions.DependencyInjection`) for chat clients, services, and configuration.
- **No hardcoded values** — endpoints, model names, deployment names, API keys, and thresholds belong in configuration (`appsettings.json`, environment variables, user secrets).
- Favor **simple, readable, maintainable** code over clever or overly abstract solutions. Optimize for someone reviewing this project as coursework.

---

## 4. Agent Design Rules

- Each agent has **one clear responsibility** (e.g., "classify support emails", "draft a reply", "review code for security issues").
- Prefer **multiple small specialized agents** over one agent with many tools and instructions.
- Keep `instructions` (system prompts) **short, specific, and unambiguous**.
- Minimize context passed to each agent — only what it needs for its task (use context isolation between agents).
- Use **structured output** for any agent whose result will be consumed by code (not just displayed to a human).

**Example:**

```csharp
// Good: focused, single-purpose agent
var classifierAgent = chatClient.CreateAgent(
    instructions: "Classify the support email into category, urgency, and sentiment. Return only the structured result.",
    name: "EmailClassifier");

// Avoid: one agent doing classification, drafting, and sending
```

---

## 5. Structured Output Rules

- Always define a **strongly typed C# record/class** for agent outputs that feed into code.
- Use **JSON schema–based structured output** (response format / schema binding) instead of asking the model to "return JSON" in free text.
- **Validate** the deserialized object before using it (required fields present, enums within range, etc.).
- Never parse free-text model responses with regex/string manipulation when a structured model is available.

**Example:**

```csharp
public record EmailClassification(
    string Category,
    string Urgency,
    string Sentiment,
    bool MissingInfo);

// Bind this type to the agent's response format / schema,
// then deserialize and validate before routing.
```

---

## 6. Tool Calling Rules

- Tools must have **clear, descriptive names** (e.g., `GetOrderStatus`, not `Tool1`).
- Every tool must include an **XML doc comment / description** explaining what it does, since this becomes the schema description sent to the model.
- Tool parameters must be **typed** (no raw `string` blobs of JSON when avoidable).
- Tool execution must **handle failures gracefully** — return a structured error result to the agent rather than throwing unhandled exceptions into the loop.
- **Log every tool call**: tool name, input parameters, result (or error), and duration.

**Example:**

```csharp
/// <summary>
/// Looks up an order by its ID and returns its current shipping status.
/// </summary>
/// <param name="orderId">The order identifier, e.g. "A-4471".</param>
[Description("Look up an order's shipping status by order ID.")]
public async Task<OrderStatusResult> GetOrderStatusAsync(string orderId)
{
    try
    {
        var status = await _orderService.GetStatusAsync(orderId);
        _logger.LogInformation("Tool GetOrderStatus succeeded for {OrderId}", orderId);
        return OrderStatusResult.Success(status);
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Tool GetOrderStatus failed for {OrderId}", orderId);
        return OrderStatusResult.Failure("Order not found or service unavailable.");
    }
}
```

---

## 7. Workflow Design Rules

- Use **MAF Workflows** for any process with a known, repeatable shape (e.g., triage pipelines, approval flows).
- Use **agents only for steps requiring judgment** (classification, drafting, summarization) — use plain code for everything else (validation, routing, formatting).
- Use **conditional / switch-case edges** to encode business rules explicitly — don't hide routing logic inside agent prompts.
- Any **risky or irreversible action** (refunds, sending external communications, deletions) must include a **human approval step** (`request_info` / human-in-the-loop) before execution.
- Design workflows as **explicit graphs**: define executors and edges clearly, and ensure the graph is buildable/validatable via `WorkflowBuilder` before running.

**Example shape:**

```
Preprocess (code)
   → Classify (agent, structured output)
      → Policy Router (code, conditional edges)
         → Escalate to Human (request_info)
         → Refund Flow (code)
         → Draft Reply (agent) → Send (code)
```

---

## 8. Code Quality Standards

- Follow **SOLID principles**.
- Use **meaningful, descriptive names** for classes, methods, and variables (avoid `data`, `temp`, `obj`, `Manager2`).
- Keep methods **small and focused** on a single task.
- **Avoid duplicate code** — extract shared logic into helpers/services.
- Add comments only to explain **why**, not **what** — avoid comments that restate the code.
- **Remove dead code** (unused methods, commented-out blocks, unused usings) before committing.

---

## 9. Logging and Observability

- Log:
  - **Agent decisions** (what an agent returned, e.g., classification result)
  - **Workflow transitions** (which executor ran, which edge fired)
  - **Tool calls** (inputs, outputs, errors, duration)
  - **Failures** at every layer
- Prefer **structured logging** (`ILogger` with named parameters, e.g., `_logger.LogInformation("Routed to {Route} for order {OrderId}", route, orderId)`) over string concatenation.
- When streaming MAF workflow events, log/print each `executor_invoked` / `executor_completed` / `output` event for visibility — no silent black-box execution.

---

## 10. Error Handling

- Handle all **expected exceptions** explicitly (network failures, malformed model output, missing configuration, tool errors).
- **Never silently swallow exceptions** — always log, and either handle, rethrow, or return a meaningful error result.
- Error messages returned to callers/agents should be **actionable** (explain what went wrong and, where possible, what to do next).
- Add **retries** only where appropriate (e.g., transient model/API call failures) — use bounded retry counts with backoff, and avoid retrying non-idempotent operations (refunds, sends) without safeguards.

---

## 11. Git Rules

- Make **small, focused commits** — one logical change per commit.
- Write **meaningful commit messages** (what changed and why, not "fix" or "update").
- Keep **pull requests small** and scoped to a single feature or fix.
- **Never commit secrets or API keys** — use `appsettings.json` placeholders, user secrets, or environment variables, and ensure sensitive files are in `.gitignore`.

---

## 12. Testing Rules

- Write **unit tests for business logic** independent of agents/LLMs (mock the agent/tool boundary).
- **Validate workflow routing logic** — test that given a classification result, the correct edge/route is selected.
- **Test structured output parsing/validation** — including malformed or incomplete model responses.
- **Test tool failure scenarios** — ensure tools return graceful errors and agents/workflows handle them without crashing.

---

## 13. Instructions for AI Coding Agents (Claude Code, Copilot, GPT, etc.)

When modifying this repository:

1. **Understand the existing architecture first** — review relevant files/folders before making changes.
2. **Reuse existing patterns** (agent setup, tool registration, workflow structure) rather than introducing new conventions.
3. **Explain major architectural changes** before implementing them (e.g., adding a new agent, restructuring a workflow).
4. **Generate production-quality code** — even though this is a learning project, code should be clean, typed, tested, and follow the rules above.
5. **Keep consistency** with existing naming, formatting, and project structure.
6. **If requirements are unclear or ambiguous, ask clarifying questions** instead of guessing or making assumptions.

---

## 14. Course Assignment Requirements

This repository implements a **Microsoft Agent Framework Workflow assignment**. Any agent working on this repo must treat the following as hard constraints — the implementation is not considered complete or correct unless all of them are satisfied.

The workflow **MUST** contain:

1. **At least 3 executors:**
   - **Preprocess Executor** (plain function executor)
   - **Classifier Executor** (agent-based executor)
   - **Router Executor**

2. **Structured Output** from the classifier, including at minimum:
   - `Category`
   - `Urgency`
   - `MissingInfo`

3. **At least 2 conditional routes** out of the router.

4. **One route must escalate to a human reviewer** (e.g., via `request_info` / human-in-the-loop).

5. **Workflow events must be streamed and displayed**, including at minimum:
   - `executor_invoked`
   - `executor_completed`
   - `output`

6. Use **`WorkflowBuilder`** to assemble and build the workflow graph.

7. Use **Microsoft Agent Framework workflows** — a custom hand-rolled `while` loop is **not** an acceptable substitute.

8. **Demonstrate one complete sample run** end-to-end (sample input → streamed events → final output).

9. **Prefer C# examples and MAF APIs** throughout, consistent with the rest of this repository.

10. **If a generated solution would violate any of the above requirements, stop and explain the violation before writing code** — do not silently produce a non-compliant implementation.
