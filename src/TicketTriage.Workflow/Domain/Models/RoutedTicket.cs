namespace TicketTriage.Workflow.Domain.Models;

/// <summary>
/// A classified ticket along with the routing decision made for it.
/// Output of <see cref="TicketTriage.Workflow.Workflow.Executors.RouterExecutor"/>;
/// the workflow's conditional edges branch on <see cref="Route"/>.
/// </summary>
public record RoutedTicket(ClassifiedTicket Classified, TicketRoute Route);
