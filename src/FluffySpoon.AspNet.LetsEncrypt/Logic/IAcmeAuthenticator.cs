using System.Threading.Tasks;
using Certes;

namespace FluffySpoon.AspNet.LetsEncrypt.Logic
{
    public interface IAcmeAuthenticator
    {
        Task<IAcmeContext> AuthenticateAsync();
    }
}