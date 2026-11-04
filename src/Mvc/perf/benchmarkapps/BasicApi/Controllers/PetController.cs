// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using BasicApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BasicApi.Controllers
{
    [ApiController]
    [Authorize("pet-store-reader")]
    [Route("/pet")]
    public class PetController : ControllerBase
    {
        public PetController(BasicApiContext dbContext)
        {
            DbContext = dbContext;
        }

        public BasicApiContext DbContext { get; }

        [HttpGet("{id}", Name = "FindPetById")]
        [ProducesResponseType(typeof(Pet),  StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Pet>> FindById(int id)
        {
            var pet = await DbContext.Pets
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (pet == null)
            {
                return new NotFoundResult();
            }

            return pet;
        }

        [AllowAnonymous]
        [HttpGet("anonymous/{id}")]
        [ProducesResponseType(typeof(Pet), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Pet>> FindByIdWithoutToken(int id)
        {
            var pet = await DbContext.Pets
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (pet == null)
            {
                return new NotFoundResult();
            }

            return pet;
        }

        [HttpGet("findByCategory/{categoryId}")]
        [ProducesResponseType(typeof(Pet), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Pet>> FindByCategory(int categoryId)
        {
            var pet = await DbContext.Pets
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Category != null && p.Category.Id == categoryId);
            if (pet == null)
            {
                return new NotFoundResult();
            }

            return pet;
        }

        [HttpGet("findByStatus")]
        [ProducesResponseType(typeof(Pet), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Pet>> FindByStatus(string status)
        {
            var pet = await DbContext.Pets
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Status == status);
            if (pet == null)
            {
                return new NotFoundResult();
            }

            return pet;
        }

        [HttpGet("findByTags")]
        [ProducesResponseType(typeof(Pet), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Pet>> FindByTags(string[] tags)
        {
            var pet = await DbContext.Pets
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Tags)
                .FirstOrDefaultAsync(p => p.Tags.Any(t => tags.Contains(t.Name)));
            if (pet == null)
            {
                return new NotFoundResult();
            }

            return pet;
        }

        [Authorize("pet-store-writer")]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> AddPet([FromBody] Pet pet)
        {
            DbContext.Pets.Add(pet);
            await DbContext.SaveChangesAsync();

            return new CreatedAtRouteResult("FindPetById", new { id = pet.Id }, pet);
        }

        [Authorize("pet-store-writer")]
        [HttpPost("add-pet")]
        public ActionResult<Pet> AddPetWithoutDb(Pet pet)
        {
            return pet;
        }

        [Authorize("pet-store-writer")]
        [HttpPut]
        public IActionResult EditPet(Pet pet)
        {
            throw new NotImplementedException();
        }

        [Authorize("pet-store-writer")]
        [HttpPost("{id}/uploadImage")]
        public IActionResult UploadImage(int id, IFormFile file)
        {
            throw new NotImplementedException();
        }

        [Authorize("pet-store-writer")]
        [HttpDelete("{id}")]
        public IActionResult DeletePet(int id)
        {
            throw new NotImplementedException();
        }
    }
}
