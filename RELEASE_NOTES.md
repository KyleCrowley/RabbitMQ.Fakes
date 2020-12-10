# Release Notes

## 2.1.2

### Bug Fixes

- Fixed a bug where messages weren't being delivered to async consumers (`IAsyncBasicConsumer`). See [#8](https://github.com/KyleCrowley/RabbitMQ.Fakes.DotNetStandard/issues/8) for an explanation of the issue.

## 2.1.1

### Bug Fixes

- Fixed a bug where messages would still be delivered to consumers even after they were cancelled (via `Model.BasicCancel(string)`). See [#6](https://github.com/KyleCrowley/RabbitMQ.Fakes.DotNetStandard/issues/8) for an explanation of the issue and [#7](https://github.com/KyleCrowley/RabbitMQ.Fakes.DotNetStandard/pull/7) for an explanation of the fix.