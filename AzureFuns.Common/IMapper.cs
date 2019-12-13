namespace AzureFuns.Common
{
    public interface IMapper<in TIn, out TOut>
    {
        TOut Map(TIn input);
    }

    public interface IMapper<in T1In, T2In, out TOut>
    {
        TOut Map(T1In input1, T2In input2);
    }
}
