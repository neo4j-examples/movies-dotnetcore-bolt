using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MoviesDotNetCore.Model;
using MoviesDotNetCore.Repositories;

namespace MoviesDotNetCore.Controllers;

[ApiController]
[Route("movie")]
public class MoviesController : ControllerBase
{
    private readonly IMovieRepository _movieRepository;

    public MoviesController(IMovieRepository movieRepository)
    {
        _movieRepository = movieRepository;
    }

    [Route("{title}")]
    [HttpGet]
    public Task<Movie> GetMovieDetails([FromRoute] string title)
    {
        if (title == "favicon.ico")
            return null;

        title = System.Net.WebUtility.UrlDecode(title);
        return _movieRepository.FindByTitle(title);
    }

    [Route("{title}/vote")]
    [HttpPost]
    public Task<int> VoteInMovie([FromRoute] string title)
    {
        title = System.Net.WebUtility.UrlDecode(title);
        return _movieRepository.VoteByTitle(title);
    }
}
