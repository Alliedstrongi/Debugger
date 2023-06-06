using Debugger.Data;
using Debugger.Models;
using Debugger.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Debugger.Services
{
    public class BTTicketHistoryService : IBTTicketHistoryService
    {
        private readonly ApplicationDbContext _context;

        public BTTicketHistoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddHistoryAsync(Ticket? oldTicket, Ticket newTicket, string userId)
        {
            try
            {
                if (oldTicket is null)
                {
                    //create a hitory item "new ticket created"
                    TicketHistory history = new TicketHistory()
                    {
                        TicketId = newTicket.Id,
                        PropertyName = string.Empty,
                        OldValue = string.Empty,
                        NewValue = string.Empty,
                        Created = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc),
                        UserId = userId,
                        Description = "New Ticket Created!"
                    };

                    _context.Add(history);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // check each property, and make a ticket history item for anything changed
                    if (!string.Equals(oldTicket.Title, newTicket.Title))
                    {
                        TicketHistory history = new TicketHistory()
                        {
                            TicketId = newTicket.Id,
                            PropertyName = nameof(Ticket.Title),
                            OldValue = oldTicket.Title,
                            NewValue = newTicket.Title,
                            Created = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc),
                            UserId = userId,
                            Description = $"Ticket title was changed to {newTicket.Title}"
                        };

                        _context.Add(history);
                    }
                    
                    if (!string.Equals(oldTicket.Description, newTicket.Description))
                    {
                        TicketHistory history = new TicketHistory()
                        {
                            TicketId = newTicket.Id,
                            PropertyName = nameof(Ticket.Description),
                            OldValue = oldTicket.Description,
                            NewValue = newTicket.Description,
                            Created = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc),
                            UserId = userId,
                            Description = $"Ticket title was changed to {newTicket.Description}"
                        };

                        _context.Add(history);
                    }
                    
                    if (oldTicket.Archived != newTicket.Archived)
                    {
                        TicketHistory history = new()
                        {
                            TicketId = newTicket.Id,
                            PropertyName = nameof(Ticket.Archived),
                            OldValue = oldTicket.Archived.ToString(),
                            NewValue = newTicket.Archived.ToString(),
                            Created = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc),
                            UserId = userId,
                            Description = newTicket.Archived == true ? "Ticket archived" : "Ticket restored"
                        };

                        _context.Add(history);
                    }

                    if (oldTicket.TicketTypeId != newTicket.TicketTypeId)
                    {
                        TicketHistory history = new()
                        {
                            TicketId = newTicket.Id,
                            PropertyName = nameof(Ticket.TicketTypeId),
                            OldValue = oldTicket.TicketType!.Name,
                            NewValue = newTicket.TicketType!.Name,
                            Created = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc),
                            UserId = userId,
                            Description = $"Ticket type was changed to {newTicket.TicketType!.Name}"
                        };

                        _context.Add(history);
                    }

                    if (oldTicket.TicketStatusId != newTicket.TicketStatusId)
                    {
                        TicketHistory history = new()
                        {
                            TicketId = newTicket.Id,
                            PropertyName = nameof(Ticket.TicketStatus),
                            OldValue = oldTicket.TicketStatus!.Name,
                            NewValue = newTicket.TicketStatus!.Name,
                            Created = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc),
                            UserId = userId,
                            Description = $"Ticket type was changed to {newTicket.TicketStatus!.Name}"
                        };

                        _context.Add(history);
                    }

                    if (oldTicket.TicketPriorityId != newTicket.TicketPriorityId)
                    {
                        TicketHistory history = new()
                        {
                            TicketId = newTicket.Id,
                            PropertyName = nameof(Ticket.TicketPriority),
                            OldValue = oldTicket.TicketPriority!.Name,
                            NewValue = newTicket.TicketPriority!.Name,
                            Created = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc),
                            UserId = userId,
                            Description = $"Ticket type was changed to {newTicket.TicketPriority!.Name}"
                        };

                        _context.Add(history);
                    }

                    if (!string.Equals(oldTicket.DeveloperUserId, newTicket.DeveloperUserId))
                    {
                        TicketHistory history = new()
                        {
                            TicketId = newTicket.Id,
                            PropertyName = "Developer",
                            OldValue = oldTicket.DeveloperUser?.FullName ?? "Unassigned",
                            NewValue = newTicket.DeveloperUser?.FullName ?? "Unassigned",
                            Created = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc),
                            UserId = userId,
                            Description = $"Ticket developer was changed to {newTicket.DeveloperUser!.FullName ?? "Unassigned"}"
                        };

                        _context.Add(history);
                    }

                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task AddHistoryAsync(int ticketId, string model, string userId)
        {
            try
            {
                Ticket? ticket = await _context.Tickets.FindAsync(ticketId);
                if (ticket == null)
                {
                    return;
                }

                string description = model.ToLower().Replace("ticket", ""); //ticketComment -> comment
                description = $"New {description} added to ticket: {ticket.Title}";

                TicketHistory history = new()
                {
                    TicketId = ticketId,
                    PropertyName= model,
                    OldValue = string.Empty,
                    NewValue = string.Empty,
                    Created = DateTime.SpecifyKind( DateTime.Now, DateTimeKind.Utc),
                    UserId = userId,
                    Description = description
                };

                _context.Add(history);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {

                throw;
            }        }

        public async Task<List<TicketHistory>> GetCompanyTicketHistoriesAsnyc(int companyId)
        {
            try
            {
                Company? company = await _context.Companies
                                                      .Include(c => c.Projects)
                                                        .ThenInclude(p => p.Tickets)
                                                            .ThenInclude(t => t.History)
                                                                .ThenInclude(h => h.User)
                                                      .FirstOrDefaultAsync(c => c.Id == companyId);

                if(company is not null)
                {
                    List<Project> projects = company.Projects.ToList();

                    List<Ticket> tickets = projects.SelectMany(p => p.Tickets).ToList();
                    List<TicketHistory> histories = tickets.SelectMany(t => t.History).ToList();

                    var test = company.Projects.SelectMany(p => p.Tickets).SelectMany(t => t.History).ToList();

                    return histories;
                }
                else
                {
                    return new List<TicketHistory>();
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<List<TicketHistory>> GetProjectTicketHistoriesAsync(int projectId, int companyId)
        {
            try
            {
                Project? project = await _context.Projects
                                                 .Include(p => p.Tickets)
                                                     .ThenInclude(t => t.History)
                                                         .ThenInclude(h => h.User)
                                                 .FirstOrDefaultAsync(p => p.Id == projectId && p.CompanyId == companyId);

                if (project is null) return new List<TicketHistory>();

                List<TicketHistory> history = project.Tickets.SelectMany(t => t.History).ToList();

                return history;
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
