# NetMQ.Communication
create the project
使用NetMQ实现Socket通讯，实现如下两种通讯模型：
1.request-reply
2.publisher-subscriber
3.beacon

request-reply<br/>
服务端和客户端无论谁先启动，效果是相同的，这点不同于Socket。
在服务端收到信息以前，程序是阻塞的，会一直等待客户端连接上来。
服务端收到信息以后，会send一个“World”给客户端。值得注意的是一定是client连接上来以后，send消息给Server，然后Server再rev然后响应client，这种一问一答式的。
如果Server先send，client先rev是会报错的。
ZMQ通信通信单元是消息，他除了知道Bytes的大小，他并不关心的消息格式。因此，你可以使用任何你觉得好用的数据格式。Xml、Protocol Buffers、Thrift、json等等。
虽然可以使用ZMQ实现HTTP协议，但是，这绝不是他所擅长的。

  数据流：client:request ->server：response [(sub) ip port] 这两个ip port相同  bind [(pub)ip port] ->client:sub [ip port]
  之后就可以client发送str server pub回client啦
【我试过直接用server 的response 发送消息至 request 直接报错，正好符合上面所说的问答流程错误】
    发布订阅模型 Publisher-Subscriber<br/>
    局域网内监听beacon广播 判断如果站点Beacon的名称是服务器的名称则自动建立连接
        发布端单向分发数据，且不关心是否把全部信息发送给订阅端。如果发布端开始发布信息时，订阅端尚未连接上来，则这些信息会被直接丢弃。
        订阅端未连接导致信息丢失的问题，可以通过与请求回应模型组合来解决。订阅端只负责接收，而不能反馈，且在订阅端消费速度慢于发布端的情况下，会在订阅端堆积数据。
        该模型主要用于数据分发。天气预报、微博明星粉丝可以应用这种经典模型。

        一个发布端可以有多个订阅端
        如果只想要接收指定的数据，订阅端必须要设置过滤字符
        订阅端设置空字符串，订阅所有的发布内容。【You can set topic an empty string to subscribe to everything】
        (发布端和订阅端的套接字绑定的地址必须一样的。比如：tcp://127.0.0.1:5556，使用tcp协议，监听端口5556)

    
    
    
    
    
     
