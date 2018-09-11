﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Controllers
{
   
    public class ValuesController : Controller
    {
        // GET api/values
        [HttpGet]
        [Route("api/values")]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        [HttpGet]
        [Route("api/values/{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        [HttpPost]
        [Route("api/values")]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        [HttpPut()]
        [Route("api/values/{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete()]
         [Route("api/values/{id}")]
        public void Delete(int id)
        {
        }
    }
}