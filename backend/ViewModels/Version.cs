using asec.Models;
using asec.Models.Archive;
using Microsoft.EntityFrameworkCore;

namespace asec.ViewModels;

public record Version
{
    public string Id { get; set; } = "";
    public string WorkId { get; set; } = "";
    public IEnumerable<string> Status { get; set; }
    public string Title { get; set; } = "";
    public string AlternativeTitle { get; set; } = "";
    public string YearOfPublication { get; set; } = "";
    public IEnumerable<string> System { get; set; }
    public string SystemRequirements { get; set; } = "";
    public string Note { get; set; } = "";


    public static Version FromDBEntity(Models.Archive.Version dbVersion)
    {
        return new() {
            Id = dbVersion.Id.ToString(),
            WorkId = dbVersion.Work?.Id.ToString() ?? "",
            Status = dbVersion.Status?.Select(s => s.Value).ToList(),
            Title = dbVersion.Title,
            AlternativeTitle = dbVersion.AlternativeTitle,
            YearOfPublication = dbVersion.YearOfPublication,
            System = dbVersion.System?.Select(s => s.Name).ToList(),
            SystemRequirements = dbVersion.SystemRequirements,
            Note = dbVersion.Note
        };
    }

    public async Task<Models.Archive.Version> ToDBEntity(AsecDBContext dbContext, bool createMissing)
    {
        List<Models.Archive.System> dbSystems = new();

        if (System != null)
        {
            var systems = dbContext.Systems.Where(s => System.Contains(s.Name));
            dbSystems.AddRange(systems);

            if (createMissing)
            {
                var textSystems = systems.Select(s => s.Name);
                var newSystems = System.Where(s => !textSystems.Contains(s)).Select(s =>
                    new Models.Archive.System {
                        Id = Guid.NewGuid(),
                        Name = s
                    }
                );
                dbSystems.AddRange(newSystems);
            }
        }

        List<Status> dbStatuses = new();

        if (Status != null)
        {
            var statuses = dbContext.Statuses.Where(s => Status.Contains(s.Value));
            dbStatuses.AddRange(statuses);

            if (createMissing)
            {
                var textStatuses = statuses.Select(s => s.Value);
                var newStatuses = Status.Where(s => !textStatuses.Contains(s)).Select(s =>
                    new Status {
                        Id = Guid.NewGuid(),
                        Value = s
                    }
                );
                dbStatuses.AddRange(newStatuses);
            }
        }

        if (!String.IsNullOrEmpty(Id))
        {
            var dbId = Guid.Parse(Id);
            var dbVersion = await dbContext.Versions.Include(v => v.Work).FirstOrDefaultAsync();
            if (dbVersion != null)
            {
                dbVersion.Status = dbStatuses;
                dbVersion.System = dbSystems;

                dbVersion.Title = Title;
                dbVersion.AlternativeTitle = AlternativeTitle;
                dbVersion.SystemRequirements = SystemRequirements;
                dbVersion.Note = Note;
                dbVersion.YearOfPublication = YearOfPublication;
            }
            throw new KeyNotFoundException("Invalid Guid");
        }
        else
        {
            var dbWork = await dbContext.Works.FindAsync(WorkId);
            return new() {
                Id = Guid.Empty,
                Title = Title,
                AlternativeTitle = AlternativeTitle,
                Status = dbStatuses,
                System = dbSystems,
                SystemRequirements = SystemRequirements,
                Note = Note,
                YearOfPublication = YearOfPublication,
                Work = dbWork
            };
        }
    }
}