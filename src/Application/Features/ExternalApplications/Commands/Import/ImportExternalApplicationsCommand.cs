using AutoMapper;
using CleanArchitecture.Application.Features.ExternalApplications.Commands.AddEdit;
using CleanArchitecture.Application.Interfaces.Repositories;
using CleanArchitecture.Application.Interfaces.Services;
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

namespace CleanArchitecture.Application.Features.ExternalApplications.Commands.Import
{
    public class ImportExternalApplicationsCommand : IRequest<Result<int>>
    {
        public UploadRequest UploadRequest { get; set; }
    }

    internal class ImportExternalApplicationsCommandHandler : IRequestHandler<ImportExternalApplicationsCommand, Result<int>>
    {
        private readonly IUnitOfWork<int> _unitOfWork;
        private readonly IExcelService _excelService;
        private readonly IMapper _mapper;
        private readonly IValidator<AddEditExternalApplicationCommand> _addExternalApplicationValidator;
        private readonly IStringLocalizer<ImportExternalApplicationsCommandHandler> _localizer;

        public ImportExternalApplicationsCommandHandler(
            IUnitOfWork<int> unitOfWork,
            IExcelService excelService,
            IMapper mapper,
            IValidator<AddEditExternalApplicationCommand> addExternalApplicationValidator,
            IStringLocalizer<ImportExternalApplicationsCommandHandler> localizer)
        {
            _unitOfWork = unitOfWork;
            _excelService = excelService;
            _mapper = mapper;
            _addExternalApplicationValidator = addExternalApplicationValidator;
            _localizer = localizer;
        }

        public async Task<Result<int>> Handle(ImportExternalApplicationsCommand request, CancellationToken cancellationToken)
        {
            var stream = new MemoryStream(request.UploadRequest.Data);
            var result = await _excelService.ImportAsync(stream, mappers: new Dictionary<string, Func<DataRow, ExternalApplication, object>>
            {
                { _localizer["Name"], (row,item) => item.Name = row[_localizer["Name"]].ToString() },
                { _localizer["Description"], (row,item) => item.Description = row[_localizer["Description"]].ToString() },
            }, _localizer["External Applications"]);

            if (result.Succeeded)
            {
                var importedExternalApplications = result.Data;
                var errors = new List<string>();
                var errorsOccurred = false;
                foreach (var externalApplication in importedExternalApplications)
                {
                    var validationResult = await _addExternalApplicationValidator.ValidateAsync(_mapper.Map<AddEditExternalApplicationCommand>(externalApplication), cancellationToken);
                    if (validationResult.IsValid)
                    {
                        if (await _unitOfWork.Repository<ExternalApplication>().Entities.Where(a => a.Id != externalApplication.Id)
                                            .AnyAsync(a => a.Name == externalApplication.Name, cancellationToken))
                        {
                            return await Result<int>.FailAsync(_localizer["External Application with this Name already exists."]);
                        }
                        externalApplication.Name = externalApplication.Name.ToUpper();
                        await _unitOfWork.Repository<ExternalApplication>().AddAsync(externalApplication);
                    }
                    else
                    {
                        errorsOccurred = true;
                        errors.AddRange(validationResult.Errors.Select(e => $"{(!string.IsNullOrWhiteSpace(externalApplication.Name) ? $"{externalApplication.Name} - " : string.Empty)}{e.ErrorMessage}"));
                    }
                }
                if (errorsOccurred)
                {
                    return await Result<int>.FailAsync(errors);
                }

                await _unitOfWork.CommitAndRemoveCache(cancellationToken, ApplicationConstants.Cache.GetAllExternalApplicationsCacheKey);
                return await Result<int>.SuccessAsync(result.Data.Count(), result.Messages[0]);
            }
            else
            {
                return await Result<int>.FailAsync(result.Messages);
            }
        }
    }
}
