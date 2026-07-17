using Order.Domain.Entities;

namespace Order.Domain.Interfaces;

public interface IOrderRepository
{
    Task AddAsync(CustomerOrder order);
    Task<IReadOnlyList<CustomerOrder>> GetByUserIdAsync(string userId);
    Task<CustomerOrder?> GetByIdAsync(Guid id);
}
