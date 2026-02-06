namespace ChatAppProj.RepositoryContracts;

public interface IGenericRepository <T> where T : class {
    void Create(T entity);
    List<T> GetAll();
    T? GetById(int id);
    void Update(T entity);
    void Delete(T entity);
}