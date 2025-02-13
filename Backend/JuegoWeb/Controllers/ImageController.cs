using JuegoWeb.Models.Database.Entities;
using JuegoWeb.Models.Dtos;
using JuegoWeb.Models.Mappers;
using JuegoWeb.Services;
using Microsoft.AspNetCore.Mvc;

namespace JuegoWeb.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ImagesController : ControllerBase
{
    private readonly ImageService _imageService;
    private readonly ImageMapper _imageMapper;

    public ImagesController(ImageService imageService, ImageMapper imageMapper)
    {
        _imageService = imageService;
        _imageMapper = imageMapper;
    }

    [HttpGet]
    public async Task<IEnumerable<ImageDto>> GetAllAsync()
    {
        IEnumerable<Image> images = await _imageService.GetAllAsync();

        return _imageMapper.ToDto(images, Request);
    }

    [HttpGet("{id}")]
    public async Task<ImageDto> GetAsync(int id)
    {
        Image image = await _imageService.GetAsync(id);

        return _imageMapper.ToDto(image, Request);
    }

    [HttpPost]
    public async Task<ActionResult<ImageDto>> InsertAsync([FromForm] CreateUpdateImageRequest createImage)
    {
        if (createImage?.File == null)
        {
            return BadRequest("El archivo es nulo.");
        }
        Image newImage = await _imageService.InsertImageAsync(createImage);

        return Created($"images/{newImage.Id}", _imageMapper.ToDto(newImage));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ImageDto>> UpdateAsync(int id, CreateUpdateImageRequest updateImage)
    {
        Image imageUpdated = await _imageService.UpdateAsync(id, updateImage);

        return Ok(_imageMapper.ToDto(imageUpdated));
    }
}