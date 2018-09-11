using AutoMapper;
using Library.API.Entities;
using Library.API.Helpers;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Controllers
{

  public class BooksController : Controller
  {
    private ILibraryRepository _libraryRepository;
    private ILogger<BooksController> _logger;
    private IUrlHelper _urlHelper;

    public BooksController(ILibraryRepository libraryRepository,
        ILogger<BooksController> logger,
        IUrlHelper urlHelper)
    {
      _logger = logger;
      _libraryRepository = libraryRepository;
      _urlHelper = urlHelper;
    }
    /// <summary>
    /// Get list of book of Author.
    /// </summary>
    /// <param name="authorId"></param>
    /// <returns></returns>
    [Route("api/authors/{authorId}/books", Name = "GetBooksForAuthor")]
    [HttpGet]
    public IActionResult GetBooksForAuthor(Guid authorId)
    {
      if (!_libraryRepository.AuthorExists(authorId))
      {
        return NotFound();
      }

      var booksForAuthorFromRepo = _libraryRepository.GetBooksForAuthor(authorId);

      var booksForAuthor = Mapper.Map<IEnumerable<BookDto>>(booksForAuthorFromRepo);

      booksForAuthor = booksForAuthor.Select(book =>
      {
        book = CreateLinksForBook(book);
        return book;
      });

      var wrapper = new LinkedCollectionResourceWrapperDto<BookDto>(booksForAuthor);

      return Ok(CreateLinksForBooks(wrapper));
    }

    /// <summary>
    /// Gets specific book of a author.
    /// </summary>
    /// <param name="authorId"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    [Route("api/authors/{authorId}/books/{id}")]
    [HttpGet]
    public IActionResult GetBookForAuthor(Guid authorId, Guid id)
    {
      if (!_libraryRepository.AuthorExists(authorId))
      {
        return NotFound();
      }

      var bookForAuthorFromRepo = _libraryRepository.GetBookForAuthor(authorId, id);
      if (bookForAuthorFromRepo == null)
      {
        return NotFound();
      }

      var bookForAuthor = Mapper.Map<BookDto>(bookForAuthorFromRepo);
      return Ok(CreateLinksForBook(bookForAuthor));
    }

    /// <summary>
    /// Adds book to the author collection.
    /// </summary>
    /// <param name="authorId"></param>
    /// <param name="book"></param>
    /// <returns></returns>
    [Route("api/authors/{authorId}/books")]
    [HttpPost]
    public IActionResult CreateBookForAuthor(Guid authorId,
        [FromBody] BookForCreationDto book)
    {
      if (book == null)
      {
        return BadRequest();
      }

      if (book.Description == book.Title)
      {
        ModelState.AddModelError(nameof(BookForCreationDto),
            "The provided description should be different from the title.");
      }

      if (!ModelState.IsValid)
      {
        // return 422
        return new UnprocessableEntityObjectResult(ModelState);
      }

      if (!_libraryRepository.AuthorExists(authorId))
      {
        return NotFound();
      }

      var bookEntity = Mapper.Map<Book>(book);

      _libraryRepository.AddBookForAuthor(authorId, bookEntity);

      if (!_libraryRepository.Save())
      {
        throw new Exception($"Creating a book for author {authorId} failed on save.");
      }

      var bookToReturn = Mapper.Map<BookDto>(bookEntity);

      return CreatedAtRoute("GetBookForAuthor",
          new { authorId = authorId, id = bookToReturn.Id },
          CreateLinksForBook(bookToReturn));
    }

    /// <summary>
    /// Deletes specific book of an author.
    /// </summary>
    /// <param name="authorId"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    [Route("api/authors/{authorId}/books/{id}")]
    [HttpDelete]
    public IActionResult DeleteBookForAuthor(Guid authorId, Guid id)
    {
      if (!_libraryRepository.AuthorExists(authorId))
      {
        return NotFound();
      }

      var bookForAuthorFromRepo = _libraryRepository.GetBookForAuthor(authorId, id);
      if (bookForAuthorFromRepo == null)
      {
        return NotFound();
      }

      _libraryRepository.DeleteBook(bookForAuthorFromRepo);

      if (!_libraryRepository.Save())
      {
        throw new Exception($"Deleting book {id} for author {authorId} failed on save.");
      }

      _logger.LogInformation(100, $"Book {id} for author {authorId} was deleted.");

      return NoContent();
    }

    /// <summary>
    /// Updates book.
    /// </summary>
    /// <param name="authorId"></param>
    /// <param name="id"></param>
    /// <param name="book"></param>
    /// <returns></returns>
    [Route("api/authors/{authorId}/books/{id}")]
    [HttpPut]
    public IActionResult UpdateBookForAuthor(Guid authorId, Guid id,
        [FromBody] BookForUpdateDto book)
    {
      if (book == null)
      {
        return BadRequest();
      }

      if (book.Description == book.Title)
      {
        ModelState.AddModelError(nameof(BookForUpdateDto),
            "The provided description should be different from the title.");
      }

      if (!ModelState.IsValid)
      {
        return new UnprocessableEntityObjectResult(ModelState);
      }


      if (!_libraryRepository.AuthorExists(authorId))
      {
        return NotFound();
      }

      var bookForAuthorFromRepo = _libraryRepository.GetBookForAuthor(authorId, id);
      if (bookForAuthorFromRepo == null)
      {
        var bookToAdd = Mapper.Map<Book>(book);
        bookToAdd.Id = id;

        _libraryRepository.AddBookForAuthor(authorId, bookToAdd);

        if (!_libraryRepository.Save())
        {
          throw new Exception($"Upserting book {id} for author {authorId} failed on save.");
        }

        var bookToReturn = Mapper.Map<BookDto>(bookToAdd);

        return CreatedAtRoute("GetBookForAuthor",
            new { authorId = authorId, id = bookToReturn.Id },
            bookToReturn);
      }

      Mapper.Map(book, bookForAuthorFromRepo);

      _libraryRepository.UpdateBookForAuthor(bookForAuthorFromRepo);

      if (!_libraryRepository.Save())
      {
        throw new Exception($"Updating book {id} for author {authorId} failed on save.");
      }

      return NoContent();
    }

    [Route("api/authors/{authorId}/books/{id}")]
    [HttpPatch]
    public IActionResult PartiallyUpdateBookForAuthor(Guid authorId, Guid id,
        [FromBody] JsonPatchDocument<BookForUpdateDto> patchDoc)
    {
      if (patchDoc == null)
      {
        return BadRequest();
      }

      if (!_libraryRepository.AuthorExists(authorId))
      {
        return NotFound();
      }

      var bookForAuthorFromRepo = _libraryRepository.GetBookForAuthor(authorId, id);

      if (bookForAuthorFromRepo == null)
      {
        var bookDto = new BookForUpdateDto();
        patchDoc.ApplyTo(bookDto, ModelState);

        if (bookDto.Description == bookDto.Title)
        {
          ModelState.AddModelError(nameof(BookForUpdateDto),
              "The provided description should be different from the title.");
        }

        TryValidateModel(bookDto);

        if (!ModelState.IsValid)
        {
          return new UnprocessableEntityObjectResult(ModelState);
        }

        var bookToAdd = Mapper.Map<Book>(bookDto);
        bookToAdd.Id = id;

        _libraryRepository.AddBookForAuthor(authorId, bookToAdd);

        if (!_libraryRepository.Save())
        {
          throw new Exception($"Upserting book {id} for author {authorId} failed on save.");
        }

        var bookToReturn = Mapper.Map<BookDto>(bookToAdd);
        return CreatedAtRoute("GetBookForAuthor",
            new { authorId = authorId, id = bookToReturn.Id },
            bookToReturn);
      }

      var bookToPatch = Mapper.Map<BookForUpdateDto>(bookForAuthorFromRepo);

      patchDoc.ApplyTo(bookToPatch, ModelState);

      // patchDoc.ApplyTo(bookToPatch);

      if (bookToPatch.Description == bookToPatch.Title)
      {
        ModelState.AddModelError(nameof(BookForUpdateDto),
            "The provided description should be different from the title.");
      }

      TryValidateModel(bookToPatch);

      if (!ModelState.IsValid)
      {
        return new UnprocessableEntityObjectResult(ModelState);
      }

      Mapper.Map(bookToPatch, bookForAuthorFromRepo);

      _libraryRepository.UpdateBookForAuthor(bookForAuthorFromRepo);

      if (!_libraryRepository.Save())
      {
        throw new Exception($"Patching book {id} for author {authorId} failed on save.");
      }

      return NoContent();
    }

    private BookDto CreateLinksForBook(BookDto book)
    {
      book.Links.Add(new LinkDto(_urlHelper.Link("GetBookForAuthor",
          new { id = book.Id }),
          "self",
          "GET"));

      book.Links.Add(
          new LinkDto(_urlHelper.Link("DeleteBookForAuthor",
          new { id = book.Id }),
          "delete_book",
          "DELETE"));

      book.Links.Add(
          new LinkDto(_urlHelper.Link("UpdateBookForAuthor",
          new { id = book.Id }),
          "update_book",
          "PUT"));

      book.Links.Add(
          new LinkDto(_urlHelper.Link("PartiallyUpdateBookForAuthor",
          new { id = book.Id }),
          "partially_update_book",
          "PATCH"));

      return book;
    }

    private LinkedCollectionResourceWrapperDto<BookDto> CreateLinksForBooks(
        LinkedCollectionResourceWrapperDto<BookDto> booksWrapper)
    {
      // link to self
      booksWrapper.Links.Add(
          new LinkDto(_urlHelper.Link("GetBooksForAuthor", new { }),
          "self",
          "GET"));

      return booksWrapper;
    }
  }
}
