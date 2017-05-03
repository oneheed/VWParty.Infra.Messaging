# RabbitMQ Toturials



# TPV2 改版目標

* 解除 zeus / tp-v 之間的 API call, 不必要的複雜度。改成 client 直接面對 TPV2, 與 Game server 溝通。 TPV2 與 zeus 應該透過標準的 token 互通會員等資訊即可，背後直接面對共同的 membership service, banking service, billing service
* 



# Message Ackowledgment
* Toturial in C#: [Work Queues](https://www.rabbitmq.com/tutorials/tutorial-two-dotnet.html)

worker 的處理模式教學。這段說明 message 分派給 worker 後，worker 處理完畢後必須回報成功。若沒接到回報則代表失敗，broker 會嘗試把這個 message 重新分配。


> In order to make sure a message is never lost, RabbitMQ supports message acknowledgments. An ack(nowledgement) is sent back from the consumer to tell RabbitMQ that a particular message has been received, processed and that RabbitMQ is free to delete it.
> 


# References

* [Reliability Guide](https://www.rabbitmq.com/reliability.html)
* [Consumer Acknowledgements and Publisher Confirms](https://www.rabbitmq.com/confirms.html)
* [Time-To-Live Extensions](https://www.rabbitmq.com/ttl.html)