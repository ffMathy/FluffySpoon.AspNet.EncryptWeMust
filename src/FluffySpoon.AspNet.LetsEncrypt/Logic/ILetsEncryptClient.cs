using System.Threading.Tasks;
using Certes;

namespace FluffySpoon.AspNet.LetsEncrypt.Logic
{
    public interface ILetsEncryptClient
    {
        Task<PlacedOrder> PlaceOrder(string[] domains, IAcmeContext acme);
        Task<PfxCertificateBytes> ValidateOrder(PlacedOrder placedOrder);
    }
}