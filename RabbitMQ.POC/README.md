



# How To Run POC Code?

1. update rabbit mq server location (change HostName and Port)
```csharp
        private static ConnectionFactory factory = new ConnectionFactory()
        {
            HostName = "172.19.3.166",
            Port = 5672
        };
```
2. login to rabbitmq management web, create exchange:
- **name**: bet
- **type**: fanout
- durability: durable
- auto delete: no
- internal: no
3. create queue: worker.data-warehouse
- **name**: worker.data-warehouse
- durability: durable
- auto delete: no
4. create queue: worker.process-transaction
- **name**: worker.process-transaction
- durability: durable
- auto delete: no
5. bind queue to exchange:
- edit exchange bet
- add binding from this exchange to queue (worker.process-transaction)
  - type: to queue
  - name: worker.process-transaction
- add binding from this exchange to queue (worker.data-warehouse)
  - type: to queue
  - name: worker.data-warehouse