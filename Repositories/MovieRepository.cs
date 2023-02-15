using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MoviesDotNetCore.Model;
using Neo4j.Driver;

namespace MoviesDotNetCore.Repositories;

public interface IMovieRepository
{
    Task<Movie> FindByTitle(string title);
    Task<int> VoteByTitle(string title);
    Task<List<Movie>> Search(string search);
    Task<D3Graph> FetchD3Graph(int limit);
}

public class MovieRepository : IMovieRepository
{
    private readonly IDriver _driver;

    public MovieRepository(IDriver driver)
    {
        _driver = driver;
    }

    public async Task<Movie> FindByTitle(string title)
    {
        if (title == "favicon.ico")
            return null;
        
        await using var session = _driver.AsyncSession(WithDatabase);

        return await session.ExecuteReadAsync(async transaction =>
        {
            var cursor = await transaction.RunAsync(@"
                        MATCH (movie:Movie {title:$title})
                        OPTIONAL MATCH (movie)<-[r]-(person:Person)
                        RETURN movie.title AS title,
                               collect({
                                   name:person.name,
                                   job: head(split(toLower(type(r)),'_')),
                                   role: reduce(acc = '', role IN r.roles | acc + CASE WHEN acc='' THEN '' ELSE ', ' END + role)}
                               ) AS cast",
                new {title}
            );
            
            return await cursor.SingleAsync(record => new Movie(
                record["title"].As<string>(),
                MapCast(record["cast"].As<List<IDictionary<string, object>>>())
            ));
        });
    }

    public async Task<int> VoteByTitle(string title)
    {
        await using var session = _driver.AsyncSession(WithDatabase);
        return await session.ExecuteWriteAsync(async transaction =>
        {
            var cursor = await transaction.RunAsync(@"
                            MATCH (m:Movie {title: $title})
                            SET m.votes = coalesce(m.votes, 0) + 1;",
                new {title}
            );

            var summary = await cursor.ConsumeAsync();
            return summary.Counters.PropertiesSet;
        });
    }

    public async Task<List<Movie>> Search(string search)
    {
        await using var session = _driver.AsyncSession(WithDatabase);
        return await session.ExecuteReadAsync(async transaction =>
        {
            var cursor = await transaction.RunAsync(@"
                        MATCH (movie:Movie)
                        WHERE toLower(movie.title) CONTAINS toLower($title)
                        RETURN movie.title AS title,
                               movie.released AS released,
                               movie.tagline AS tagline,
                               movie.votes AS votes",
                new {title = search}
            );

            return await cursor.ToListAsync(record => new Movie(
                record["title"].As<string>(),
                Tagline: record["tagline"].As<string>(),
                Released: record["released"].As<long>(),
                Votes: record["votes"]?.As<long>()
            ));
        });
    }

    public async Task<D3Graph> FetchD3Graph(int limit)
    {
        await using var session = _driver.AsyncSession(WithDatabase);
        return await session.ExecuteReadAsync(async transaction =>
        {
            var cursor = await transaction.RunAsync(@"
                        MATCH (m:Movie)<-[:ACTED_IN]-(p:Person)
                        WITH m, p
                        ORDER BY m.title, p.name
                        RETURN m.title AS title, collect(p.name) AS cast
                        LIMIT $limit",
                new {limit}
            );

            var nodes = new List<D3Node>();
            var links = new List<D3Link>();

            // IAsyncEnumerable available from Version 5.5 of .NET Driver.
            await foreach (var record in cursor)
            {
                var movie = new D3Node(record["title"].As<string>(), "movie");
                var movieIndex = nodes.Count;
                nodes.Add(movie);
                foreach (var actorName in record["cast"].As<IList<string>>())
                {
                    var actor = new D3Node(actorName, "actor");
                    var actorIndex = nodes.IndexOf(actor);
                    actorIndex = actorIndex == -1 ? nodes.Count : actorIndex;
                    nodes.Add(actor);
                    links.Add(new D3Link(actorIndex, movieIndex));
                }
            }

            return new D3Graph(nodes, links);
        });
    }

    private static IEnumerable<Person> MapCast(IEnumerable<IDictionary<string, object>> persons)
    {
        return persons
            .Select(dictionary =>
                new Person(
                    dictionary["name"].As<string>(),
                    dictionary["job"].As<string>(),
                    dictionary["role"].As<string>()
                )
            ).ToList();
    }

    private static void WithDatabase(SessionConfigBuilder sessionConfigBuilder)
    {
        var neo4jVersion = Environment.GetEnvironmentVariable("NEO4J_VERSION") ?? "";
        if (!neo4jVersion.StartsWith("4"))
            return;

        sessionConfigBuilder.WithDatabase(Database());
    }

    private static string Database()
    {
        return Environment.GetEnvironmentVariable("NEO4J_DATABASE") ?? "movies";
    }
}