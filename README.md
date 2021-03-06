# AsyncTcp
### 异步TCP服务器、TCP客户端

​	支持自定义通信协议，可以作为简单的数据交换通信信道，如接口通信。如果想要传输大文件，需要实现自定义的通信协议，如果想要稳定，还需要支持断点续传。

​	刚开始引入TCP服务器功能时，觉得supersocket挺好用的，也支持自定义通信协议，但是使用了一段时间后发现自定义的数据过滤器经常丢失数据，实在搞不清楚他的数据过虑器该怎么实现，所以自己实现一个通信协议。



#### 目前已实现的通信协议

1. 开始标志-数据-结束标志

   开始标志：垂直制表符，一个字节

   ```c#
   byte[] m_beginMark = new byte[] { (byte)((char)11) };
   ```

   结束标记：文件分割符+回车符，两个字节

   ```c#
   byte[] m_endMark = new byte[] { (byte)((char)28), (byte)((char)13) };
   ```

   

2. 开始标志-数据长度-数据-校验码。

   开始标志：垂直制表符，一个字节

   ```c#
   byte[] m_beginMark = new byte[] { (byte)((char)11) };
   ```

   数据长度：4个字节，包含整个消息结构长度：开始标志、数据长度、消息体、校验码构成的字节长度。

   效验码：一个字节，这个版本的校验码只是简单的对消息体字节数组中每个字节“异或”操作后的结果，自定义的版本可以替换为任何想要的方法。
   
   


#### 依赖项

​	AsyncTcp当前支持的.NET Framework版本是4.5。




#### 引用说明

​	最简单的引用方式就是直接使用[NuGet package 'AsyncTcpEx'](https://www.nuget.org/packages/AsyncTcpEx/):

​	如果是使用Visual Studio的包管理器控制台，则直接运行以下命令：
```
PM > Install-Package AsyncTcpEx -Version 1.0.0.2
```

​	引用该库后也可以自定义自己的通信协议，继承IReceiveDataFilter即可，实现思路其实也是参考了supersocket的思路，只是搞不清楚他的数据过虑器该怎么实现。

​	**具体实现可参考已有的两个通信协议：**

1. [AsyncTcp/AsyncTcpServer/BeginEndFilter.cs](https://github.com/huangjunjiegdmc/AsyncTcp/blob/master/AsyncTcp/BeginEndFilter.cs)
2. [AsyncTcp/AsyncTcpServer/BeginDataLenVerifyFilter.cs](https://github.com/huangjunjiegdmc/AsyncTcp/blob/master/AsyncTcp/BeginDataLenVerifyFilter.cs)