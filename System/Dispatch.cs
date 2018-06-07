namespace DomainLogic.System
{
    public delegate void Dispatch<in TInput>(TInput input);
}