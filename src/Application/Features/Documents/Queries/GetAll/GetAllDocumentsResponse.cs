﻿using System;

namespace CleanArchitecture.Application.Features.Documents.Queries.GetAll
{
    public class GetAllDocumentsResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public bool IsPublic { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string URL { get; set; }
        public string ExternalApplication { get; set; }
        public string DocumentType { get; set; }
        public string ExternalId { get; set; }
    }
}