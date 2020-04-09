# AsyncTcp
异步TCP服务器，异步TCP客户端，已实现的通信协议：开始标志-数据-结束标志、开始标志-数据长度-数据-校验码。

刚开始引入TCP服务器功能时，觉得supersocket挺好用的，也支持自定义通信协议，但是使用了一段时间后发现自定义的数据过滤器经常丢失数据，实在搞不清楚他的数据过虑器该怎么实现，迫不得已自己实现一个通信协议。

目前已实现的通信协议：开始标志-数据-结束标志、开始标志-数据长度-数据-校验码。
引用该库后也可以自定义自己的通信协议，继承IReceiveDataFilter即可，实现思路其实也是参考了supersocket的思路，
只是搞不清楚他的数据过虑器该怎么实现，具体实现可参考已有的两个通信协议AsyncTcp/AsyncTcpServer/BeginEndFilter.cs、AsyncTcp/AsyncTcpServer/BeginDataLenVerifyFilter.cs。