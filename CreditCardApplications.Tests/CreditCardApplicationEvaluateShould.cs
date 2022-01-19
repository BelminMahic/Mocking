using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using Xunit;

namespace CreditCardApplications.Tests
{
    public class CreditCardApplicationEvaluateShould
    {
        [Fact]
        public void AcceptHighIncomeApplications()
        {
            Mock<IFrequentFlyerNumberValidator> mockValidator = new Mock<IFrequentFlyerNumberValidator>(); // thanks to this i got a proper interface for DI to use

            var sut = new CreditCardApplicationEvaluator(mockValidator.Object);

            var application = new CreditCardApplication { GrossAnnualIncome = 100_000 };

            CreditCardApplicationDecision decision = sut.Evaluate(application);
            Assert.Equal(CreditCardApplicationDecision.AutoAccepted, decision);
        }

        [Fact]
        public void ReferYoungApplications()
        {
            Mock<IFrequentFlyerNumberValidator> mockValidator = new Mock<IFrequentFlyerNumberValidator>();
            mockValidator.Setup(x => x.ServiceInformation.Licence.LicenceKey).Returns("OK"); // just to pass

            var sut = new CreditCardApplicationEvaluator(mockValidator.Object); // now we need for mock methods to retrieve a needed value, because boolean value is false by default

            var application = new CreditCardApplication { Age = 19 };

            CreditCardApplicationDecision decision = sut.Evaluate(application);
            Assert.Equal(CreditCardApplicationDecision.ReferredToHuman, decision);

        }
        [Fact]
        public void DeclineLowIncomeApplications()
        {
            Mock<IFrequentFlyerNumberValidator> mockValidator = new Mock<IFrequentFlyerNumberValidator>();
            var sut = new CreditCardApplicationEvaluator(mockValidator.Object);

            //mockValidator.Setup(x => x.IsValid("x")).Returns(true); // also instead of passing this type of value that needs to be changed all the time, we can make our tests more dynamic = Argument Matching
            //mockValidator.Setup(x => x.IsValid(It.IsAny<string>())).Returns(true); // when any string is passed into isValid method
            //mockValidator.Setup(x => x.IsValid(It.Is<string>(number => number.StartsWith("y")))).Returns(true); // return a value when specific is true
            //mockValidator.Setup(x => x.IsValid(It.IsInRange<string>("a", "z", Moq.Range.Inclusive))).Returns(true); // return a value from a specific range
            //mockValidator.Setup(x => x.IsValid(It.IsIn("x", "y", "z"))).Returns(true); // It.IsIn for IEnumerables or params array for set of values that will be matched
            //mockValidator.DefaultValue = DefaultValue.Mock; // can be useful, but hides the problems under the hood. Alternative is to specify return value explicitly
            mockValidator.Setup(x => x.IsValid(It.IsRegex("[a-z]"))).Returns(true);
            mockValidator.Setup(x => x.ServiceInformation.Licence.LicenceKey).Returns("OK"); // just to pass


            var application = new CreditCardApplication { GrossAnnualIncome = 19_999, Age = 42, FrequentFlyerNumber = "a" };

            CreditCardApplicationDecision decision = sut.Evaluate(application);

            Assert.Equal(CreditCardApplicationDecision.AutoDeclined, decision);
        }

        [Fact]
        public void ReferInvalidFrequentFlyerApplications()
        {
            // if we have invalid flyer number then the app gets refered

            Mock<IFrequentFlyerNumberValidator> mockValidator = new Mock<IFrequentFlyerNumberValidator>();
            mockValidator.Setup(x => x.ServiceInformation.Licence.LicenceKey).Returns("OK"); // just to pass
            mockValidator.Setup(x => x.IsValid(It.IsAny<string>())).Returns(false);

            var sut = new CreditCardApplicationEvaluator(mockValidator.Object);

            var application = new CreditCardApplication();

            CreditCardApplicationDecision decision = sut.Evaluate(application);

            Assert.Equal(CreditCardApplicationDecision.ReferredToHuman, decision);

        }
        [Fact]
        public void DeclineLowOutIncomeApplications()
        {
            Mock<IFrequentFlyerNumberValidator> mockValidator = new Mock<IFrequentFlyerNumberValidator>();
            var sut = new CreditCardApplicationEvaluator(mockValidator.Object);

            bool isValid = true;
            mockValidator.Setup(x => x.IsValid(It.IsAny<string>(), out isValid));

            var application = new CreditCardApplication
            {
                GrossAnnualIncome = 19_999,
                Age = 42
            };

            CreditCardApplicationDecision decision = sut.EvaluateUsingOut(application);

            Assert.Equal(CreditCardApplicationDecision.AutoDeclined, decision);
        }

        [Fact]
        public void ReferWhenLicenceKeyExpired()
        {
            //Mock<IFrequentFlyerNumberValidator> mockValidator = new Mock<IFrequentFlyerNumberValidator>();

            //mockValidator.Setup(x => x.LicenceKey).Returns(GetLicenceKeyExpiryString); // prima i funkciju

            //var mockLicenceData = new Mock<ILicenceData>();
            //mockLicenceData.Setup(x => x.LicenceKey).Returns("EXPIRED"); // od ovoga pa sve na dole

            //var mockServiceInformation = new Mock<IServiceInformation>();
            //mockServiceInformation.Setup(x => x.Licence).Returns(mockLicenceData.Object);

            //Mock<IFrequentFlyerNumberValidator> mockValidator = new Mock<IFrequentFlyerNumberValidator>();
            //mockValidator.Setup(x => x.ServiceInformation).Returns(mockServiceInformation.Object);
            //mockValidator.Setup(x => x.IsValid(It.IsAny<string>())).Returns(true);

            // New version instead of all the setups above

            Mock<IFrequentFlyerNumberValidator> mockValidator = new Mock<IFrequentFlyerNumberValidator>();
            mockValidator.Setup(x => x.ServiceInformation.Licence.LicenceKey).Returns("EXPIRED");
            mockValidator.Setup(x => x.IsValid(It.IsAny<string>())).Returns(true);
            var sut = new CreditCardApplicationEvaluator(mockValidator.Object);
            
            var application = new CreditCardApplication { Age = 42 };
            CreditCardApplicationDecision decision = sut.Evaluate(application);

            Assert.Equal(CreditCardApplicationDecision.ReferredToHuman, decision);
        }

        [Fact]
        public void UseDetailedLookupForOlderApplications()
        {
            var mockValidator = new Mock<IFrequentFlyerNumberValidator>();
            mockValidator.SetupAllProperties(); // reason it failed before was that this method needs to be called before any other setups !

            mockValidator.Setup(x => x.ServiceInformation.Licence.LicenceKey).Returns("OK");
            //mockValidator.SetupProperty(x => x.ValidationMode); // no Returns needed here
            //If we have more needed to track

            var sut = new CreditCardApplicationEvaluator(mockValidator.Object);
            var application = new CreditCardApplication { Age = 30 };

            sut.Evaluate(application);

            Assert.Equal(ValidationMode.Detailed, mockValidator.Object.ValidationMode);
        }

        [Fact]
        public void ValidateFrequentFlyerNumberForLowIncomeApps()
        {
            var mockValidator = new Mock<IFrequentFlyerNumberValidator>();
            mockValidator.Setup(x => x.ServiceInformation.Licence.LicenceKey).Returns("OK");

            var sut = new CreditCardApplicationEvaluator(mockValidator.Object);
            var application = new CreditCardApplication
            {
                FrequentFlyerNumber = "q"
            };

            sut.Evaluate(application);

            mockValidator.Verify(x => x.IsValid(It.IsAny<string>()), Times.Once); // for expected invocation
        }
        // Sometimes we want to see if it is not called
        [Fact]
        public void NotValidateFrequentFlyerNumberForHighIncomeApps()
        {
            var mockValidator = new Mock<IFrequentFlyerNumberValidator>();
            mockValidator.Setup(x => x.ServiceInformation.Licence.LicenceKey).Returns("OK");

            var sut = new CreditCardApplicationEvaluator(mockValidator.Object);
            var application = new CreditCardApplication
            {
                GrossAnnualIncome = 99_000
            };

            sut.Evaluate(application);

            mockValidator.Verify(x => x.IsValid(It.IsAny<string>())); // dont want assert, use Verify :)
        }

        [Fact]
        public void CheckLicenceKeyForLowIncomeApps()
        {
            var mockValidator = new Mock<IFrequentFlyerNumberValidator>();
            mockValidator.Setup(x => x.ServiceInformation.Licence.LicenceKey).Returns("OK");

            var sut = new CreditCardApplicationEvaluator(mockValidator.Object);
            var application = new CreditCardApplication
            {
                GrossAnnualIncome = 99_000
            };
            sut.Evaluate(application);
            mockValidator.VerifyGet(x => x.ServiceInformation.Licence.LicenceKey);
        }

        [Fact]
        public void SetDetailedLookupForOlderApps()
        {
            var mockValidator = new Mock<IFrequentFlyerNumberValidator>();
            mockValidator.Setup(x => x.ServiceInformation.Licence.LicenceKey).Returns("OK");

            var sut = new CreditCardApplicationEvaluator(mockValidator.Object);
            var application = new CreditCardApplication
            {
                Age = 30
            };

            sut.Evaluate(application);
            //mockValidator.VerifySet(x => x.ValidationMode = ValidationMode.Detailed);
            mockValidator.VerifySet(x => x.ValidationMode = It.IsAny<ValidationMode>()); // even though i change logic in the code, with this it will pass, reasons: It.IsAny
        }
        // throwing exceptions from mock objects
        [Fact]
        public void ReferWhenFrequentFlyerValidationError()
        {
            var mockValidator = new Mock<IFrequentFlyerNumberValidator>();
            mockValidator.Setup(x => x.ServiceInformation.Licence.LicenceKey).Returns("OK");
            //mockValidator.Setup(x => x.IsValid(It.IsAny<string>())).Throws<Exception>();
            mockValidator.Setup(x => x.IsValid(It.IsAny<string>())).Throws(new Exception("Custom exception message"));

            var sut = new CreditCardApplicationEvaluator(mockValidator.Object);
            var application = new CreditCardApplication
            {
                Age = 30
            };

            var decision = sut.Evaluate(application);
            Assert.Equal(CreditCardApplicationDecision.ReferredToHuman, decision);
        }
        // mock objects to raise events
        [Fact]
        public void IncrementLookupCount()
        {
            var mockValidator = new Mock<IFrequentFlyerNumberValidator>();

            mockValidator.Setup(x => x.ServiceInformation.Licence.LicenceKey).Returns("OK");
            //mockValidator.Setup(x => x.IsValid(It.IsAny<string>())).Throws<Exception>();
            mockValidator.Setup(x => x.IsValid(It.IsAny<string>())).Returns(true).Raises(x => x.ValidatorLookupPerformed += null, EventArgs.Empty);

            var sut = new CreditCardApplicationEvaluator(mockValidator.Object);
            var application = new CreditCardApplication
            {
                FrequentFlyerNumber = "x",
                Age = 25
            };

            sut.Evaluate(application);

           // mockValidator.Raise(x => x.ValidatorLookupPerformed += null, EventArgs.Empty);

            Assert.Equal(1, sut.ValidatorLookupCount);

        }
        //return different results if method is called repeadetly
        [Fact]
        public void ReferInvalidFrequentFlyerApps_ReturnValueSequences()
        {
            var mockValidator = new Mock<IFrequentFlyerNumberValidator>();

            mockValidator.Setup(x => x.ServiceInformation.Licence.LicenceKey).Returns("OK");
            //mockValidator.Setup(x => x.IsValid(It.IsAny<string>())).Throws<Exception>();
            //mockValidator.Setup(x => x.IsValid(It.IsAny<string>())).Returns(false);

            mockValidator.SetupSequence(x => x.IsValid(It.IsAny<string>())).Returns(false).Returns(true);

            var sut = new CreditCardApplicationEvaluator(mockValidator.Object);
            var application = new CreditCardApplication
            {
                Age = 25
            };

            CreditCardApplicationDecision decision1 = sut.Evaluate(application);
            Assert.Equal(CreditCardApplicationDecision.ReferredToHuman, decision1);

            CreditCardApplicationDecision decision2 = sut.Evaluate(application);
            Assert.Equal(CreditCardApplicationDecision.AutoDeclined, decision2);
        }
        // verify it was called with multiple sequences
        [Fact]
        public void ReferInvalidFrequentFlyerApss_MultipleCallsSequence()
        {
            var mockValidator = new Mock<IFrequentFlyerNumberValidator>();
            mockValidator.Setup(x => x.ServiceInformation.Licence.LicenceKey).Returns("OK");

            var frequentFlyerNumberPassed = new List<string>();
            mockValidator.Setup(x => x.IsValid(Capture.In(frequentFlyerNumberPassed)));

            var sut = new CreditCardApplicationEvaluator(mockValidator.Object);
            var application1 = new CreditCardApplication
            {
                Age = 25,
                FrequentFlyerNumber = "aa"
            };

            var application2 = new CreditCardApplication
            {
                Age = 25,
                FrequentFlyerNumber = "bb"
            };

            var application3 = new CreditCardApplication
            {
                Age = 25,
                FrequentFlyerNumber = "cc"
            };

            sut.Evaluate(application1);
            sut.Evaluate(application2);
            sut.Evaluate(application3);

            // assert that IsValid was called three times with aa, bb, cc
            Assert.Equal(new List<string> { "aa", "bb", "cc" }, frequentFlyerNumberPassed);
        }
        // partial mocks
        [Fact]
        public void ReferFraudRisk()
        {
            var mockValidator = new Mock<IFrequentFlyerNumberValidator>();
            var mockFraudLookup = new Mock<FraudLookup>();
           
            //mockFraudLookup.Setup(x => x.IsFraudRisk(It.IsAny<CreditCardApplication>())).Returns(true);

            mockFraudLookup.Protected().Setup<bool>("CheckApplication", ItExpr.IsAny<CreditCardApplication>()).Returns(true);

            var sut = new CreditCardApplicationEvaluator(mockValidator.Object, mockFraudLookup.Object);

            var application = new CreditCardApplication();

            CreditCardApplicationDecision decision = sut.Evaluate(application);

            Assert.Equal(CreditCardApplicationDecision.ReferredToHumanFraudRisk, decision);
        }
        [Fact]
        public void LinqUsage()
        {
            var mockValidator = new Mock<IFrequentFlyerNumberValidator>();

            mockValidator.Setup(x => x.ServiceInformation.Licence.LicenceKey).Returns("OK");

            var frequentFlyerNumberPassed = new List<string>();
            mockValidator.Setup(x => x.IsValid(Capture.In(frequentFlyerNumberPassed)));

            var sut = new CreditCardApplicationEvaluator(mockValidator.Object);
            var application1 = new CreditCardApplication
            {
                Age = 25                
            };

            CreditCardApplicationDecision decision = sut.Evaluate(application1);
            Assert.Equal(CreditCardApplicationDecision.AutoDeclined, decision);

        }
        string GetLicenceKeyExpiryString()
        {
            return "EXPIRED";
        }
    }
}
/*
 MockBehaviour.Strict => throw an exception if a mocked method is called but has not been setup.
 MockBehaviour.Loose => never throw exception, even if a mocked method is called but has not been setup returns default values for value types, null for reference types, empty array / enumerable
 MockBehaviour.Default => default behaviour if none specified (MockBehaviour.Loose)

 Difference between Loose and Strict
 Loose : 
    - Fewer lines of setup code
    - Setup only what is relevant
    - Default values for not setted things
    - Less brittle tests(brittle = krhak, lomljiv)
    - Existing tests continue to work

 Strict :
    - more lines of setup code
    - amy have to setup irrelevant things
    - have to setup each called method
    - more brittle tests
    - existing tests may break

 Matching ref Arguments with Moq
    public class Person
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public interface IGateway
{
    bool Execute(ref Person person);
}
public class Gateway : IGateway
{
    private readonly IGateway _gateway;
    public Gateway(IGateway gateway)
    {
        _gateway = gateway;
    }
    public bool Execute(ref Person person)
    {
        int returnCode = _gateway.Execute(ref person);
        return returnCode == 0;
    }
}

    Ways of working
1.) No explicit setup
    sut.Process(person);
2.) Match Specific ref Object Instance
    // Two Person instances
    mockGateway.Setup(x=>x.Execute(ref person1)).Returns(-1);
    sut.Process(person1); // -1
    sut.Process(person2); // 0
3.) Match any assignment compatible type
    mockGateway.Setup(x=>x.Execute(ref It.Ref<Person>.IsAny)).Returns(-1);
    Calling is the same as above, now it can take even more than one

By default mock properties wont remember changes made into their values

 */

