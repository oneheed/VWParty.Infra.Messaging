[![build status](https://gitlab.66.net/msa/VWParty.Infra.Messaging/badges/develop/build.svg)](https://gitlab.66.net/msa/VWParty.Infra.Messaging/commits/develop) (branch: develop)
----

# 如何透過 NuGet 取得這個套件?

套件的發行都透過公司的 NuGet server 進行。這個專案也已正確設定好 CI / CD 流程。只要程式碼推送 (push)
到 GitLab 就會觸發 CI-Runner, 自動編譯及打包套件，推送到 NuGet server.

公司的 NuGet Server URL:

develop branch: 自動發行 BETA 版本 (pre-release)
master branch:  自動發行正式版 (release)


如何取得最新版 (release)
```powershell
install-package VWParty.Infra.Messaging
```

如何取得最新版 (pre-release, beta)
```powershell
install-package VWParty.Infra.Messaging -pre
```


# Solution 說明

這個 repository 包含一個 visual studio solution, 裡面的每個 project 用途說明如下:

- /POC/BetTransactions.Client
- /POC/BetTransactions.Worker

> 使用 SDK 的程式碼範例。Client 是發送端，會不斷地發送訊息到 ```tp-transaction``` 這個 exchange, Worker 則是接收端
> 的範例，會不斷地用 Pull API 從 ```bet_test``` 這個 queue 提取訊息出來處理，處理完畢會回應 return message & ack。  
> 
> Client 支援兩種模式: async (預設) / sync, 使用方式是 ```BetTransactions.Client.exe [async | sync]```, sync mode 會等待
> Worker 回應的訊息，而 async mode 不會  
> Worker 預設會建立 10 個執行續平行處理 message, 按下 ENTER 會啟用正常中止的程序 (會把處理到一半的 message 處理完才結束 Worker)。
> 若需要調整執行緒數量，需要調整 source code


- /POC/RabbitMQ.POC

> 直接使用 RabbitMQ.Client .NET SDK 的使用範例，參考用

- /POC/Zeus.Messaging.Client
- /POC/Zeus.Messaging.Worker

> 舊版 (MSMQ) SDK 的使用範例，測試相容性用的 code. 同樣的 Client 是發送端，Worker 是接收處理端

- /VWParty.Infra.Messaging

> SDK 本身的 project



# VWParty.Infra.Messaging SDK 使用說明

目前我們底層採用的訊息平台是 RabbitMQ, 他提供幾種基礎的元件，可以讓我們來規畫整套系統的資訊流。
簡單介紹我們常用的元件:

1. Queue:
實際的Queue可以儲存訊息。訊息被放進去 (publish) queue 會按照先後順序排隊。等待著被另一端按照順序
一個一個取出處理。取出的 message 處理完之後必須回報是否處理成功 (ack), 若沒有回報就斷線，或是超出
預期時間，則這筆 message 會重新回到 queue 內重新發送。

1. Exchange:
Exchange任務就像郵差，只負責分派 message。Exchange接收到message後，會根據routing key來決定要轉發
到哪裡。轉發的目的地可能是另一個 exchange 或是 queue。隨著不同的 exchange / queue 的組態設定不同,
訊息可能同時被分派到多個 queue, 也有可能直接被丟棄。


要存取 RabbitMQ, 其實官方就已經提供 .NET 版本的 SDK 了 (RabbitMQ.Client)，這份 SDK 的主要目的是
簡化我們內部使用 RabbitMQ 的進入門檻。我把使用的方式分為兩大類:

1. 單向傳遞: 發送端送出訊息後 **不需要** 等待回應
2. 雙向傳遞: 發送端送出訊息後 **要** 等待回應

每一種應用，又分為發送端 (Client) 與接受訊息的處理端 (Server)。因此 SDK 有四個主要的類別，是開發人員
必須留意的:

1. (單向) 發送端: ```class MessageClientBase```
2. (單向) 接收端: ```class MessageServerBase```
3. (雙向) 發送端: ```class RpcClientBase```
4. (雙向) 接收端: ```class RpcServerBase```


# 單向處理範例 MessageClientBase / MessageServerBase

用於傳遞訊息後就不須理會他的狀況。發送端送完訊息，確認訊息成功進入 queue 後就可以離開，繼續做
其他任務了。訊息會留在 queue 之中等待被領取處理。

## RabbitMQ 設定

標準做法是 RabbitMQ 建立 Exchange 負責接收 message, MessageClientBase 就負責把訊息推送到 Exchange 的任務。
在 RabbitMQ Exchange 上面定義 routing 機制來決定該如何把 message 分派到 queue，MessageServerBase 則負責如何
處理送來的 message。

此案例中，我們建立一個 Exchange:
- name: "tp-transaction"
- type: "direct"

同時建立一組 Queue:
- name: "bet-test"

接著把 Queue 綁定 (bind) 到 exchange:
- routing key: "letou"

之後只要補上負責傳送與接收訊息的程式碼就可以運作了。


## 發送端的程式碼 (衍生自: MessageClientBase)

按照 SDK 的規格，你需要定義一個自己的 Client, 做法是:

1. 定義訊息類別 (例如: ```class BetTransactionMessage```), 需繼承自 ```InputMessageBase```
2. 定義新類別 (例如: ```BetMessageClient```)，繼承自 ```MessageClientBase<BetTransactionMessage>```
3. 定義 public 的 constructor (例如: ```public BetMessageClient() : base("tp-transaction", "direct") { }```)

完整範例程式碼如下:

BetTransactionMessage.cs
```csharp
    public class BetTransactionMessage : InputMessageBase
    {
        // key fields
        public string Id { get; set; }
        public string BrandId { get; set; }
        public string PlatformCode { get; set; }
        public string GameId { get; set; }
        public string AccountId { get; set; }
        public string BetId { get; set; }
        public string TransactionNumber { get; set; }
        public string RoundId { get; set; }

        // content fields
        public decimal TransactionAmoung { get; set; }
        public DateTime TransactionDateTime { get; set; }
        public BetTransactionTypeEnum TransactionType { get; set; }
        public bool IsTransactionFinished { get; set; }


        // property fields
        public string RawRequestJson { get; set; }
        public DateTime CreateTime { get; set; }
    }
```

BetMessageClient.cs
```csharp
    public class BetMessageClient : MessageClientBase<BetTransactionMessage>
    {
        public BetMessageClient() : base("tp-transaction", "direct")
        {

        }
    }
```


使用時的程式碼:
```csharp

	BetMessageClient bmc = new BetRpcClient();
	BetTransactionMessage btm = new BetTransactionMessage()
	{
		Id = string.Format("{0}-{1}", i, Guid.NewGuid())
	};
	bmc.PublishAsync("letou", btm).Wait();

```

按照上述過程，就可以成功地發送訊息到 RabbitMQ Exchange (tp-transaction)。若有給定正確的 routing key, 則訊息會成功的被送到指定的 Queue (bet-test)。





## 處理端的程式碼 (衍生自: MessageServerBase)


按照 SDK 的規格，你需要定義一個自己的 Server, 做法是:

1. 定義訊息類別 (例如: ```class BetTransactionMessage```), 需繼承自 ```InputMessageBase```
這部分需與前面發送端的定義一至。最佳做法是放到同一個 library 引用他。
2. 定義新類別 (例如: ```BetMessageServer```)，繼承自 ```MessageServerBase<BetTransactionMessage>```
3. 定義 public 的 constructor (例如: ```public BetMessageServer() : base("bet-test") { }```)

完整範例程式碼如下:

BetTransactionMessage.cs (同上，略)


BetMessageServer.cs
```csharp
    public class BetMessageServer: MessageServerBase<BetTransactionMessage>
    {
        public BetMessageServer() : base("bet-test")
        {

        }
        protected override void ExecuteSubscriberProcess(BetTransactionMessage message, LogTrackerContext logtracker)
        {
			 // do something...
            return;
        }
    }
```

其中 ExecuteSubscriberProcess( ) 的內容，就是接收到訊息時要做的動作。請示情況填入正確的程式碼。




使用時的程式碼:
```csharp

    using (BetMessageServer bms = new BetMessageServer())
    {
        var x = brs.StartWorkersAsync(10);

        Console.WriteLine("PRESS [ENTER] To Exit...");
        Console.ReadLine();

        bms.StopWorkers();
        x.Wait();

        Console.WriteLine("Shutdown complete.");
    }

```

使用時的程式碼稍作解釋:

1. 建立 ```BetMessageServer``` 物件
2. 啟動 Server, 我們可以指定 Server 要開啟幾個 threads 來處理。啟動請呼叫 ```StartWorkersAsync()```, 他是非同步呼叫。Stop 後才會 await 到結果。
3. 若要中止 Server, 請呼叫 ```StopWorkers()```, 他會觸發正常中止的程序，處理到的任務會繼續完成，但是不會在處理新任務。全部結束後 StartWorkersAsync().Wait() 就會 RETURN。

如此按照這範例就可以簡單且高效率的處理 message
















# 雙向處理範例 RpcClientBase / RpcServerBase


(TBD)




























----
  
  
  
  
  
  
  



# 其他舊的文件內容 (以下請忽略)


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




# POC-SETUP

1. 建立 exchange: tp-transaction, direct mode, durable
2. 建立 queue: bet_test, durable, NO autodelete
3. (1) 與 (2) 之間建立 binding, routing key: letou

