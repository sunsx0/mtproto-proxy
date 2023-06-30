# mtproto-proxy
MTProto proxy server
Subscribe us [@tgsocks](https://t.me/tgsocks)

# Requirements
* Install .NETCore 7.0 first

# Installation
## Clone this repository
```
git clone https://github.com/freepvps/mtproto-proxy.git
cd mtproto-proxy
```

## Build project
navigate to the **tgsocks** folder and run these commands
```
dotnet restore src
dotnet build src -c Release
```

## Change config
* Secret - your secret
* Servers - configur listners
  * Host - binding host (0.0.0.0 for ipv4 all, :: for ipv6 all)
  * Port - binding port
  * Backlog - socket backlog
* DataCentres - Telegram datacentre's addresses
* ConnectionsPerThread - How many connections works one one thread
* ReceiveBufferSize - How many bytes server can receive in one Receive call
* SelectTimeout - Select waiting timeout in microsecond (1 second = 1000000 microseconds)
* Secret - your secret (hex-string)
```
vi src/tgsocks/config.json
```

## Start proxy
simply click on **tgsocks.exe**
