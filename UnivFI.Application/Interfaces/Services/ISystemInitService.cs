using System.Threading.Tasks;

namespace UnivFI.Application.Interfaces.Services
{
    public interface ISystemInitService
    {
        /// <summary>
        /// Initializes the admin account and Administrator role if they don't exist
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        Task InitializeAdminAccountAsync();
    }
}