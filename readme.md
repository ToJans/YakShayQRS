# An attempt for message-less CQRS (under the hood messages are still used)

This is an attempt for a CQRS reference app with the minimum amount of code

## Example implementation domain

### Blog post with an overview:

[Here](http://www.corebvba.be/blog/post/CQRS-for-the-lazy-ass.aspx)

### Source
[Here](https://github.com/ToJans/MinimalisticCQRS/tree/master/MinimalisticCQRS/Domain)

### Sequence diagrams
 
* [Register Account](http://www.websequencediagrams.com/?lz=bm90ZSBsZWZ0IG9mIEdVSToKICBSZWdpc3RlciBhbiBhY2NvdW50CmVuZCBub3RlCkdVSS0-QQAQBlVuaXF1ZW5lc3NWYWxpZGF0b3I6ADUIABwHID8KAF4Fb3ZlciAAHRoKIAAVCE51bWJlciB1AFYFID8AcAkgCm9wdCAAbAYKICAAeww6AIEpCQCBEQcKICBvcHQgQWxsb3dlZAogIABVCS0AKgsAgT4HAIFmCAAkBWVuZAplbmQKCg&s=earth)
* [Deposit Cash](http://www.websequencediagrams.com/?lz=bm90ZSBsZWZ0IG9mIEdVSToKICBEZXBvc2l0IHNvbWUgY2FzaAplbmQgbm90ZQpHVUktPkFjY291bnQ6ACEIQ2FzaApvcHQgQWxsb3dlZAogIAAcBy0AIgtBbW91bnQAVQdlZAplbmQKCg&s=earth)
* [Withdraw Cash](http://www.websequencediagrams.com/?lz=bm90ZSBsZWZ0IG9mIEdVSToKICBXaXRoZHJhdyBzb21lIGNhc2gKZW5kIG5vdGUKR1VJLT5BY2NvdW50OgAhCUNhc2gKb3B0IEFsbG93ZWQKICAAHQctACMLQW1vdW50AFYIbgplbmQKCg&s=earth)
* [Transfer an amount](http://www.websequencediagrams.com/?lz=bm90ZSBsZWZ0IG9mIEdVSToKICBUcmFuc2ZlciBhbiBhbW91bnQgZnJvbSBBIHRvIEIKZW5kIG5vdGUKR1VJLT5BY2MAHQVBOgAvCEEALgUKb3B0IFZhbGlkIGZvciBzb3VyY2UKICAAJwktADAMIAAvBldpdGhkcmF3bgATFgCBEQhTYWdhOgCBHglQcm9jZXNzZWRPblMAVw8AJQwAgSEKQjogAC0HAIFiCE9uVGFyZ2V0CiAgAIEpDnQADwgAgS8KQgCBLgtCAIExCERlcG9zaXRlZAAaEUdVSQCBIQpDb21wbGUAIQZlbHNlAIJhCmludgBmDyAAXhgAgXAWRmFpbGVkAIE-CwCBZh9BOiBDYW5jZWwAg2YIAIFYDQCDCRUAgU8WQQCBVBEAUgUAgV4GbmQKZW5kCg&s=earth)
