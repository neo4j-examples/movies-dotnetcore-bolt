using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
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
    private readonly QueryConfig _queryConfig;

    public MovieRepository(IDriver driver)
    {
        var versionStr = Environment.GetEnvironmentVariable("NEO4J_VERSION") ?? "";
        if( double.TryParse(versionStr, out var version) && version >= 4.0)
        {
            _queryConfig = new QueryConfig(database: Environment.GetEnvironmentVariable("NEO4J_DATABASE") ?? "movies");
        }
        else
        {
            _queryConfig = new QueryConfig();
        }

        _driver = driver;
    }

    public async Task<Movie> FindByTitle(string title)
    {
        var (queryResults, _) = await _driver
            .ExecutableQuery(@"
                MATCH (movie:Movie {title:$title})
                OPTIONAL MATCH (movie)<-[r]-(person:Person)
                RETURN movie.title AS title,
                       collect({
                           name:person.name,
                           job: head(split(toLower(type(r)),'_')),
                           role: reduce(acc = '', role IN r.roles | acc + CASE WHEN acc='' THEN '' ELSE ', ' END + role)}
                       ) AS cast")
            .WithParameters(new { title })
            .WithConfig(_queryConfig)
            .ExecuteAsync();

        return queryResults
            .Select(
                record => new Movie(
                    record["title"].As<string>(),
                    MapCast(record["cast"].As<List<IDictionary<string, object>>>())))
            .Single();
    }

    public async Task<int> VoteByTitle(string title)
    {
        var (_, summary) = await _driver
            .ExecutableQuery(@"
                MATCH (m:Movie {title: $title})
                SET m.votes = coalesce(m.votes, 0) + 1")
            .WithParameters(new { title })
            .WithConfig(_queryConfig)
            .ExecuteAsync();

        return summary.Counters.PropertiesSet;
    }

    public async Task<List<Movie>> Search(string search)
    {
        var (queryResults, _) = await _driver
            .ExecutableQuery(@"
                MATCH (movie:Movie)
                WHERE toLower(movie.title) CONTAINS toLower($title)
                RETURN movie.title AS title,
                       movie.released AS released,
                       movie.tagline AS tagline,
                       movie.votes AS votes")
            .WithParameters(new { title = search })
            .WithConfig(_queryConfig)
            .ExecuteAsync();

        return queryResults
            .Select(
                record => new Movie(
                    record["title"].As<string>(),
                    Tagline: record["tagline"].As<string>(),
                    Released: record["released"].As<long>(),
                    Votes: record["votes"]?.As<long>()))
            .ToList();
    }

    public async Task<D3Graph> FetchD3Graph(int limit)
    {
        var (queryResults, _) = await _driver
            .ExecutableQuery(@"
                MATCH (m:Movie)<-[:ACTED_IN]-(p:Person)
                WITH m, p
                ORDER BY m.title, p.name
                RETURN m.title AS title, collect(p.name) AS cast
                LIMIT $limit")
            .WithParameters(new { limit })
            .WithConfig(_queryConfig)
            .ExecuteAsync();

        var nodes = new List<D3Node>();
        var links = new List<D3Link>();

        foreach (var record in queryResults)
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
    }

    private static IEnumerable<Person> MapCast(IEnumerable<IDictionary<string, object>> persons)
    {
        return persons
            .Select(
                dictionary =>
                    new Person(
                        dictionary["name"].As<string>(),
                        dictionary["job"].As<string>(),
                        dictionary["role"].As<string>()))
            .ToList();
    }
}
