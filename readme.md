This project used to be called [MinimalisticCQRS](https://github.com/ToJans/MinimalisticCQRS/), but has evolved into
# YakShayQRS - architectural CQRS masturbation

The name is a reference to the commonly know term Yak Shaving (i.e. doing things that in 
essence are not necessary to reach your goal).

It is a CQRS example that supports both the conventional way of applying events/commands :
'''
Apply(new RegisterAccount {
  OwnerName = "Tom Janssens",
  AccountNumber="123-456789-01",
  AccountId="account/1"
});
'''

or through method calls (without event classes, using dynamic dispatching):

'''
ApplyEvent.RegisterAccount(OwnerName: "Tom Janssens", AccountNumber:"123-456789-01", AccountId: "account/1");
'''

This allows you to use messaging without explicitly declaring message classes....

Handle with care, as it migh blow up in your face !!!