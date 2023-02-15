using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MoviesDotNetCore.Model;
using MoviesDotNetCore.Repositories;

namespace MoviesDotNetCore.Controllers;

[ApiController]
[Route("graph")]
public class GraphController
{
    private readonly IMovieRepository _movieRepository;

    public GraphController(IMovieRepository movieRepository)
    {
        _movieRepository = movieRepository;
    }

    [HttpGet]
    public Task<D3Graph> FetchD3Graph([FromQuery] int limit)
    {
        return _movieRepository.FetchD3Graph(limit <= 0 ? 50 : limit);
    }
}