using CleanArchitecture.Application.Requests.SMS;
using CleanArchitecture.Application.Interfaces.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Interfaces.Services
{
    public interface ISMSService
    {
        Task SendAsync (SMSRequest request);
    }
}
