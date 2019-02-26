using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Scopely.Elasticsearch
{
    class TargetBlockWithCompletion<T> : ITargetBlock<T>
    {
        readonly ITargetBlock<T> _target;

        public TargetBlockWithCompletion(ITargetBlock<T> target, Task completion)
        {
            _target = target;
            Completion = completion;
        }

        public Task Completion { get; }

        public void Complete() => _target.Complete();

        public void Fault(Exception exception) => _target.Fault(exception);

        public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, T messageValue, ISourceBlock<T> source, bool consumeToAccept)
            => _target.OfferMessage(messageHeader, messageValue, source, consumeToAccept);
    }
}
