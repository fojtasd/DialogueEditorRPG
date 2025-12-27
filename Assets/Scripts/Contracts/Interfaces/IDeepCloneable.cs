namespace Contracts.Interfaces {
    public interface IDeepCloneable<out T> {
        T DeepClone();
    }
}