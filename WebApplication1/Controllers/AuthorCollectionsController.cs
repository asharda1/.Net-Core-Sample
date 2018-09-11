using AutoMapper;
using Library.API.Entities;
using Library.API.Helpers;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Controllers
{
    
//[Route("api/authorcollections")]
/// <summary>
/// APIs for collection of Authors
/// </summary>
    public class AuthorCollectionsController : Controller
    {
        private ILibraryRepository _libraryRepository;

        public AuthorCollectionsController(ILibraryRepository libraryRepository)
        {
            _libraryRepository = libraryRepository;
        }

        /// <summary>
        /// Create author collection.
        /// </summary>
        /// <param name="authorCollection"> IEnumerable collection of authors</param>
        /// <returns></returns>
        [HttpPost]
        [Route("api/authorcollections")]
        public IActionResult CreateAuthorCollection(
            [FromBody] IEnumerable<AuthorForCreationDto> authorCollection)
        {
            //Check for collection input
            if (authorCollection == null)
            {
                return BadRequest();
            }

            //Map entity
            var authorEntities = Mapper.Map<IEnumerable<Author>>(authorCollection);

            //Add authors to repository
            foreach (var author in authorEntities)
            {
                _libraryRepository.AddAuthor(author);
            }

            if (!_libraryRepository.Save())
            {
                throw new Exception("Creating an author collection failed on save.");
            }

            var authorCollectionToReturn = Mapper.Map<IEnumerable<AuthorDto>>(authorEntities);
            var idsAsString = string.Join(",",
                authorCollectionToReturn.Select(a => a.Id));

            return CreatedAtRoute("GetAuthorCollection",
                new { ids = idsAsString },
                authorCollectionToReturn);
            //return Ok();
        }

        // (key1,key2, ...)
        [Route("api/authorcollections/{ids}")]
        [HttpGet]
        public IActionResult GetAuthorCollection(
            [ModelBinder(BinderType = typeof(ArrayModelBinder))] IEnumerable<Guid> ids)
        {           
            if (ids == null)
            {
                return BadRequest();
            }

            var authorEntities = _libraryRepository.GetAuthors(ids);

            if (ids.Count() != authorEntities.Count())
            {
                return NotFound();
            }

            var authorsToReturn = Mapper.Map<IEnumerable<AuthorDto>>(authorEntities);
            return Ok(authorsToReturn);
        }
    }
}
