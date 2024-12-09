//Imports

namespace SampleCode
{
    public class CartHeaderRepository: RepositoryBase<CartHeaderRepository>, ICartHeaderRepository
    {
        public MenuItemChangeHeaderRepository(MMDbContext context) : base(context) { }

        /**
         * Get Header by Id
        **/
        public MenuItemChangeHeader FindById(int id) => context.MenuItemChangeHeader.Where(
               field => field.Id == id).FirstOrDefault();

        /**
         * Call a Stored Procedure to Archive Cart
        **/
        public int Archive(UserEntity userEntity)
        {
            var OwnerUserId = new SqlParameter("@OwnerUserId", userEntity.UserId);
            return context.Database.ExecuteSqlRaw("api.p_CartArchive @OwnerUserId", OwnerUserId);
        }


    }
}