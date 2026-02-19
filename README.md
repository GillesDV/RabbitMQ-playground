# RabbitMQ-playground

Just a little RabbitMQ exercise to write the concepts out. 3 Console projects

- OrderPublisher, gets incoming orders (from 3rd party or whoever) & publishes messages internally via RabbitMQ
- OrderProcessor, receives messages and does something with them. 
   - Negative value gets send straight to DLQ 
   - Odd value gets retried X amount of times with a delay of 5 seconds, before 
- NotificationService, silently listens to the event and sends an email about it (aka logs it in this case)

## TIL 
- you have to manually set up your DLQ exchanges, as well as a retry mechanism that makes sense (eg: retry X amount of times after Y seconds, not just spam infinitely and hope it works) 
- it's easy to create infinite loops and/or send messages to nowhere
- How to handle a message: 
  - Ack = message is ok
  - Nack = message is NOK. Gets sent to the designated x-death-letter-exchange (be it DLQ or a retry mechanism)
  - Cancel = subsubscribe from your queue
- ExchangeType: 
  - Fanout: sends to every queue or exchange. Routing key is ignored
  - Topic: uses pattern matching to decide who gets messages. eg: `orders.*`
  - Direct: sends to the exact matching queue or exchange, based on the routing key. Default option.
