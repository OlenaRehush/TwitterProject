using System;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Linq.Expressions;

namespace Twitter.DAL.Repository
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private  TwitterContext _context;
        protected DbSet<T> objectSet;
        public Repository(TwitterContext context)
        {
            _context = context;
            objectSet = context.Set<T>();
        }

        public IQueryable<T> GetAll(Expression<Func<T, bool>> filter = null)
        {
            return filter != null
                ? _context.Set<T>().Where(filter)
                : _context.Set<T>();
        }

        public T Get(Expression<Func<T,bool>> filter)
        {
            if(filter == null)
            {
                throw new ArgumentNullException("Filter cannot be null!");
            }


            return _context.Set<T>().FirstOrDefault(filter);
        }

        public void Create(T entity)
        {

            _context.Set<T>().Add(entity);

        }

        public void Update(T entity)
        {
            _context.Set<T>().AddOrUpdate(entity);
     
        }

        public void Delete(T entity)
        {
            if (_context.Entry(entity).State == EntityState.Detached)
            {
                _context.Set<T>().Attach(entity);
            }

            _context.Set<T>().Remove(entity);
        }

        public void Delete(Expression<Func<T, bool>> filter)
        {
            IQueryable<T> records = from x in _context.Set<T>().Where<T>(filter) select x;

            foreach (T record in records)
            {
                _context.Set<T>().Remove(record);
            }

            _context.SaveChanges();
        }

        public void DeleteById(int id)
        {
            _context.Set<T>().Remove(_context.Set<T>().Find(id));
            _context.SaveChanges();
        }

        public T GetById(int id)
        {
            return _context.Set<T>().Find(id);
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}
