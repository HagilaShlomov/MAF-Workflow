# MAF Workflow — Support Ticket Triage

A small Microsoft Agent Framework (MAF) workflow that triages incoming support
tickets: a code executor normalizes the ticket, an LLM agent classifies it
(category / urgency / missing info), a code router applies business rules to
pick a route, and the ticket is then either auto-replied (LLM-drafted),
processed as a refund, or escalated to a human reviewer via a
`request_info` human-in-the-loop port.


## Architecture

```
src/TicketTriage.Workflow/
├── Domain/
│   ├── Models/        # Plain records/enums — no agent or MAF dependencies
│   └── Validation/     # TicketClassificationValidator (pure business logic)
├── Agents/             # AIAgent factories (Classifier, DraftReply)
├── Infrastructure/      # ChatClientFactory, ChatModelOptions (model config)
├── Workflow/
│   ├── Executors/      # The 7 graph nodes (ReflectingExecutor)
│   └── SupportTriageWorkflowFactory.cs  # WorkflowBuilder graph definition
└── Program.cs          # DI wiring, sample run, event streaming

tests/TicketTriage.Workflow.Tests/
├── RouterExecutorTests.cs              # routing rule unit tests
└── TicketClassificationValidatorTests.cs
```

- **Domain** has zero dependencies on agents/LLMs/MAF — it's plain C#.
- **Agents** are single-purpose, structured-output (`ChatResponseFormat.ForJsonSchema`) `ChatClientAgent`s.
- **Workflow/Executors** wrap agents or plain code as `ReflectingExecutor<TSelf>` + `IMessageHandler<TIn, TOut>` nodes.
- **Infrastructure** centralizes the `IChatClient` configuration (provider-agnostic OpenAI-compatible endpoint — works with Groq, Azure OpenAI, GitHub Models, etc.).

## Workflow diagram

```mermaid
flowchart TD
    IN([IncomingTicket]) --> Preprocess

    subgraph Pipeline["Linear pipeline"]
        Preprocess["Preprocess\n(code)\nIncomingTicket → SupportTicket"]
        Classifier["Classifier\n(agent, structured output)\nSupportTicket → ClassifiedTicket\nCategory / Urgency / MissingInfo / Reasoning"]
        Router["Router\n(code, conditional edges)\nClassifiedTicket → RoutedTicket"]
        Preprocess --> Classifier --> Router
    end

    Router -- "MissingInfo\nor Urgency=Critical" --> HumanPort["request_info port\n'HumanReviewRequest'\nRoutedTicket → HumanReviewDecision"]
    Router -- "Category=Refund" --> Refund["Refund\n(code, terminal)\nRoutedTicket → WorkflowOutcome"]
    Router -- "else" --> DraftReply["DraftReply\n(agent)\nRoutedTicket → ReplyToSend"]

    HumanPort -- "reviewer responds\n(SendResponseAsync)" --> HumanDecision["HumanDecision\n(code, terminal)\nHumanReviewDecision → WorkflowOutcome"]
    DraftReply --> SendReply["SendReply\n(code, terminal)\nReplyToSend → WorkflowOutcome"]

    Refund --> OUT([WorkflowOutcome])
    SendReply --> OUT
    HumanDecision --> OUT

    classDef agent fill:#e0d4f7,stroke:#7a4fc9,color:#000;
    classDef code fill:#d4f0d4,stroke:#4f9c4f,color:#000;
    classDef port fill:#fde8c8,stroke:#d99a3f,color:#000;
    classDef io fill:#f0f0f0,stroke:#999,color:#000;

    class Preprocess,Router,Refund,SendReply,HumanDecision code;
    class Classifier,DraftReply agent;
    class HumanPort port;
    class IN,OUT io;
```

## Executors

| # | Executor | Type | In → Out | Responsibility |
|---|---|---|---|---|
| 1 | `PreprocessExecutor` | code | `IncomingTicket` → `SupportTicket` | Assign ticket ID, timestamp, trim input |
| 2 | `ClassifierExecutor` | agent | `SupportTicket` → `ClassifiedTicket` | LLM classification (Category/Urgency/MissingInfo/Reasoning), validated |
| 3 | `RouterExecutor` | code | `ClassifiedTicket` → `RoutedTicket` | Apply routing business rules (3 routes) |
| 4 | `RefundExecutor` | code, terminal | `RoutedTicket` → `WorkflowOutcome` | Initiate refund (deterministic) |
| 5 | `DraftReplyExecutor` | agent | `RoutedTicket` → `ReplyToSend` | LLM drafts an email reply |
| 6 | `SendReplyExecutor` | code, terminal | `ReplyToSend` → `WorkflowOutcome` | "Send" the drafted reply |
| 7 | `HumanDecisionExecutor` | code, terminal | `HumanReviewDecision` → `WorkflowOutcome` | Resolve outcome after human review |

### Routing rules (`RouterExecutor.DetermineRoute`)

1. `MissingInfo == true` **or** `Urgency == Critical` → **HumanReview** (escalate via `request_info`)
2. else `Category == Refund` → **Refund**
3. else → **AutoReply** (draft + send)

## Configuration

The chat model is configured under the `"ChatModel"` section
(`Endpoint`, `ApiKey`, `ModelId`) in
[appsettings.json](src/TicketTriage.Workflow/appsettings.json), environment
variables, or user secrets — **never commit a real API key**.
`appsettings.json` ships with `ApiKey: ""`.

Set your key via user secrets (run from `src/TicketTriage.Workflow/`):

```bash
dotnet user-secrets set "ChatModel:Endpoint" "https://api.groq.com/openai/v1"
dotnet user-secrets set "ChatModel:ApiKey" "<your-groq-api-key>"
dotnet user-secrets set "ChatModel:ModelId" "openai/gpt-oss-120b"
```

Any OpenAI Chat Completions–compatible endpoint works (Groq, Azure OpenAI's
`/openai/v1` surface, GitHub Models, etc.) — `ChatClientFactory` just needs
Endpoint + ApiKey + ModelId.

## Running

```bash
dotnet run --project src/TicketTriage.Workflow
```

This runs 3 sample tickets — one per route (AutoReply, Refund, HumanReview) —
streaming `executor_invoked` / `executor_completed` / `request_info` / `output`
events to the console. For the HumanReview route, the program simulates a
reviewer approving the escalation via `SendResponseAsync`.

## Testing

```bash
dotnet test tests/TicketTriage.Workflow.Tests
```

Covers `RouterExecutor.DetermineRoute` routing rules and
`TicketClassificationValidator` (including malformed/incomplete classifier output).
