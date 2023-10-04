using AutoMapper;
using CleanArchitecture.Application.Interfaces.Repositories;
using CleanArchitecture.Application.Interfaces.Services;
using CleanArchitecture.Application.Models.Sgcd;
using CleanArchitecture.Application.Requests;
using CleanArchitecture.Domain.Entities.Sgcd;
using CleanArchitecture.Shared.Constants.Application;
using CleanArchitecture.Shared.Wrapper;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Features.DocumentTypes.Commands.Import
{
    public class ImportDocumentTypesCommand : IRequest<Result<int>>
    {
        public UploadRequest UploadRequest { get; set; }
    }

    internal class ImportDocumentTypesCommandHandler : IRequestHandler<ImportDocumentTypesCommand, Result<int>>
    {
        private readonly IUnitOfWork<int> _unitOfWork;
        private readonly IExcelService _excelService;
        private readonly IMapper _mapper;
        private readonly IValidator<ImportDocumentTypes> _importDocumentTypeValidator;
        private readonly IStringLocalizer<ImportDocumentTypesCommandHandler> _localizer;

        public ImportDocumentTypesCommandHandler(
            IUnitOfWork<int> unitOfWork,
            IExcelService excelService,
            IMapper mapper,
            IValidator<ImportDocumentTypes> importDocumentTypeValidator,
            IStringLocalizer<ImportDocumentTypesCommandHandler> localizer)
        {
            _unitOfWork = unitOfWork;
            _excelService = excelService;
            _mapper = mapper;
            _importDocumentTypeValidator = importDocumentTypeValidator;
            _localizer = localizer;
        }

        public async Task<Result<int>> Handle(ImportDocumentTypesCommand request, CancellationToken cancellationToken)
        {
            var stream = new MemoryStream(request.UploadRequest.Data);
            var result = (await _excelService.ImportAsync(stream, mappers: new Dictionary<string, Func<DataRow, DocumentType, object>>
            {
                { _localizer["Name"], (row,item) => item.Name = row[_localizer["Name"]].ToString() },
                { _localizer["Description"], (row,item) => item.Description = row[_localizer["Description"]].ToString() },
                { _localizer["Format"], (row,item) => item.Format = row[_localizer["Format"]].ToString() },
                { _localizer["Application Id"], (row,item) => item.ExternalApplicationId = int.Parse(row[_localizer["Application Id"]].ToString()) }
            }, _localizer["Document Types"]));

            if (result.Succeeded)
            {
                var importedDocumentTypes = result.Data;
                var errors = new List<string>();
                var errorsOccurred = false;
                foreach (var documentType in importedDocumentTypes)
                {
                    var validationResult = await _importDocumentTypeValidator.ValidateAsync(_mapper.Map<ImportDocumentTypes>(documentType), cancellationToken);
                    if (validationResult.IsValid)
                    {
                        documentType.Name = documentType.Name.ToUpper();
                        if (await _unitOfWork.Repository<DocumentType>().Entities.Where(t => t.Id != documentType.Id)
                            .AnyAsync(t => t.Name == documentType.Name, cancellationToken))
                        {
                            return await Result<int>.FailAsync(_localizer["Document type with this Name already exists."]);
                        }
                        var externalApp = await _unitOfWork.Repository<ExternalApplication>().GetByIdAsync(documentType.ExternalApplicationId);
                        if (externalApp == null)
                            return await Result<int>.FailAsync(_localizer["External Application Not Found!"]);

                        if (!ApplicationConstants.Formats.DocumentFormats.CheckFormat(documentType.Format))
                            return await Result<int>.FailAsync(_localizer["Document Type Format is not valid!"]);

                        documentType.Format = documentType.Format.ToUpper();
                        await _unitOfWork.Repository<DocumentType>().AddAsync(documentType);
                    }
                    else
                    {
                        errorsOccurred = true;
                        errors.AddRange(validationResult.Errors.Select(e => $"{(!string.IsNullOrWhiteSpace(documentType.Name) ? $"{documentType.Name} - " : string.Empty)}{e.ErrorMessage}"));
                    }
                }
                if (errorsOccurred)
                {
                    return await Result<int>.FailAsync(errors);
                }
                await _unitOfWork.CommitAndRemoveCache(cancellationToken, ApplicationConstants.Cache.GetAllDocumentTypesCacheKey);
                return await Result<int>.SuccessAsync(result.Data.Count(), result.Messages[0]);
            }
            else
            {
                return await Result<int>.FailAsync(result.Messages);
            }
        }
    }
}
