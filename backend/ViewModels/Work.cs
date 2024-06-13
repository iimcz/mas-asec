using asec.Models;
using asec.Models.Archive;
using Microsoft.EntityFrameworkCore;

namespace asec.ViewModels;

public record Work
{
    public string Id { get; set; } = "";
    public IEnumerable<string> Status { get; set; }
    public string Title { get; set; } = "";
    public string AlternativeTitle { get; set; } = "";
    public string Subheading { get; set; } = "";
    public string Description { get; set; } = "";
    public IEnumerable<string> Classification { get; set; }
    public string YearOfPublication { get; set; } = "";
    public IEnumerable<string> Genre { get; set; }
    public IEnumerable<string> TimeClassification { get; set; }
    public IEnumerable<string> LocationClassification { get; set; }
    public string Note { get; set; } = "";

    public static Work FromDbEntity(asec.Models.Archive.Work dbWork)
    {
        return new() {
            Id = dbWork.Id.ToString(),
            Title = dbWork.Title,
            AlternativeTitle = dbWork.AlternativeTitle,
            Subheading = dbWork.Subheading,
            Status = dbWork.Status?.Select(status => status.Value).ToList(),
            Description = dbWork.Description,
            Classification = dbWork.Classification?.Select(classi => classi.Value).ToList(),
            YearOfPublication = dbWork.YearOfPublication,
            Genre = dbWork.Genre?.Select(genre => genre.Name).ToList(),
            TimeClassification = dbWork.TimeClassification?.Select(timecl => timecl.Time).ToList(),
            LocationClassification = dbWork.LocationClassification?.Select(locaccl => locaccl.Location).ToList(),
            Note = dbWork.Note
        };
    }

    public async Task<asec.Models.Archive.Work> ToDbEntity(AsecDBContext dbContext, bool createMissing)
    {
        List<Genre> dbGenres = new();

        if (Genre != null)
        {
            var genres = dbContext.Genres.Where(g => Genre.Contains(g.Name));
            dbGenres.AddRange(genres);

            if (createMissing)
            {
                var textGenres = genres.Select(g => g.Name);
                var newGenres = Genre.Where(g => !textGenres.Contains(g)).Select( g =>
                    new Genre() {
                        Id = Guid.NewGuid(),
                        Name = g
                    }
                );
                dbGenres.AddRange(newGenres);
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
                var newStatuses = Status.Where(s => !textStatuses.Contains(s)).Select( s =>
                    new Status() {
                        Id = Guid.NewGuid(),
                        Value = s
                    }
                );
                dbStatuses.AddRange(newStatuses);
            }
        }

        List<Classification> dbClassifications = new();

        if (Classification != null)
        {
            var classifications = dbContext.Classifications.Where(c => Classification.Contains(c.Value));
            dbClassifications.AddRange(classifications);

            if (createMissing)
            {
                var textClassifications = classifications.Select(c => c.Value);
                var newClassifications = Classification.Where(c => !textClassifications.Contains(c)).Select( c =>
                    new Classification() {
                        Id = Guid.NewGuid(),
                        Value = c
                    }
                );
                dbClassifications.AddRange(newClassifications);
            }
        }

        List<TimeClassification> dbTimeClassifications = new();

        if (TimeClassification != null)
        {
            var timeClassifications = dbContext.TimeClassifications.Where(c => TimeClassification.Contains(c.Time));
            dbTimeClassifications.AddRange(timeClassifications);

            if (createMissing)
            {
                var textTimeClassifications = timeClassifications.Select(c => c.Time);
                var newTimeClassifications = TimeClassification.Where(c => !textTimeClassifications.Contains(c)).Select(c =>
                    new TimeClassification() {
                        Id = Guid.NewGuid(),
                        Time = c
                    }
                );
                dbTimeClassifications.AddRange(newTimeClassifications);
            }
        }

        List<LocationClassification> dbLocationClassifications = new();

        if (LocationClassification != null)
        {
            var locationClassifications = dbContext.LocationClassifications.Where(c => LocationClassification.Contains(c.Location));
            dbLocationClassifications.AddRange(locationClassifications);

            if (createMissing)
            {
                var textLocationClassifications = locationClassifications.Select(c => c.Location);
                var newLocationClassifications = LocationClassification.Where(c => !textLocationClassifications.Contains(c)).Select( c =>
                    new LocationClassification() {
                        Id = Guid.NewGuid(),
                        Location = c
                    }
                );
                dbLocationClassifications.AddRange(newLocationClassifications);
            }
        }

        if (!String.IsNullOrEmpty(Id))
        {
            var dbId = Guid.Parse(Id);
            var dbWork = await dbContext.Works.FindAsync(dbId);
            if (dbWork != null)
            {
                dbWork.Status = dbStatuses;
                dbWork.Genre = dbGenres;
                dbWork.Classification = dbClassifications;
                dbWork.TimeClassification = dbTimeClassifications;
                dbWork.LocationClassification = dbLocationClassifications;

                dbWork.Title = Title;
                dbWork.AlternativeTitle = AlternativeTitle;
                dbWork.Subheading = Subheading;
                dbWork.Description = Description;
                dbWork.Note = Note;
                dbWork.YearOfPublication = YearOfPublication;

                return dbWork;
            }
            throw new KeyNotFoundException("Invalid Guid");
        }
        else
        {
            return new() {
                Id = Guid.Empty,
                Title = Title,
                AlternativeTitle = AlternativeTitle,
                Subheading = Subheading,
                Description = Description,
                Status = dbStatuses,
                Genre = dbGenres,
                Classification = dbClassifications,
                TimeClassification = dbTimeClassifications,
                LocationClassification = dbLocationClassifications,
                Note = Note,
                YearOfPublication = YearOfPublication,
            };
        }
    }
}