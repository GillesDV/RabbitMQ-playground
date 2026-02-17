# RabbitMQ-playground

Just a little RabbitMQ exercise to write the concepts out. 3 Console projects

- OrderPublisher, gets incoming orders (from 3rd party or whoever) & publishes messages internally via RabbitMQ
- OrderProcessor, receives messages and does something with them (in this case just ACK or NACK)
- NotificationService, silently listens to the event and sends an email about it (aka logs it in this case)
