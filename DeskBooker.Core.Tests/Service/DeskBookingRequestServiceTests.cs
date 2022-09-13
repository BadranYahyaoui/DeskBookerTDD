using Bogus;
using DeskBooker.Core.DataInterface;
using DeskBooker.Core.Domain;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace DeskBooker.Core.Service
{
    public class DeskBookingRequestServiceTests
  {
    private readonly DeskBookingRequest _request;
    private readonly List<Desk> _availableDesks;
    private readonly Mock<IDeskBookingRepository> _deskBookingRepositoryMock;
    private readonly Mock<IDeskRepository> _deskRepositoryMock;
    private readonly DeskBookingRequestService _service;

    public DeskBookingRequestServiceTests()
    {

            #region Generating Fake Data with BOGUS

            _request = new Faker<DeskBookingRequest>()// generate fake BookingRequest
                            .RuleFor(dbr => dbr.FirstName, f => f.Person.FirstName)
                            .RuleFor(dbr => dbr.LastName, f => f.Person.LastName)
                            .RuleFor(dbr => dbr.Email, f => f.Person.Email)
                            .RuleFor(dbr => dbr.Date, f => f.Date.Future())
                            .Generate();
            _availableDesks = new Faker<Desk>()//generate fake Desk list
                                   .RuleFor(d => d.Id, f => f.Random.Int(1, 100))
                                   .RuleFor(d => d.Description, f => f.Lorem.Paragraph(2))
                                   .Generate(5);
            #endregion
            #region Mocking Repositories with MOCK4
            _deskBookingRepositoryMock = new Mock<IDeskBookingRepository>();//deskBooking mock
            _deskRepositoryMock = new Mock<IDeskRepository>();
            _deskRepositoryMock.Setup(x => x.GetAvailableDesks(_request.Date)) //desk mock
              .Returns(_availableDesks);//mock generated desks
            #endregion

            _service = new DeskBookingRequestService( //creating service with mocked repositories
        _deskBookingRepositoryMock.Object, _deskRepositoryMock.Object); 


        }

    [Fact]
    public void ShouldReturnDeskBookingResultWithRequestValues()
    {
      // Act
      DeskBookingResult result = _service.BookDesk(_request);

      // Assert
      Assert.NotNull(result);
      Assert.Equal(_request.FirstName, result.FirstName);
      Assert.Equal(_request.LastName, result.LastName);
      Assert.Equal(_request.Email, result.Email);
      Assert.Equal(_request.Date, result.Date);
    }

    [Fact]
    public void ShouldThrowExceptionIfRequestIsNull()
    {
      var exception = Assert.Throws<ArgumentNullException>(() => _service.BookDesk(null));

      Assert.Equal("request", exception.ParamName);
    }

    [Fact]
    public void ShouldSaveDeskBooking()
    {
      DeskBooking savedDeskBooking = null;
      _deskBookingRepositoryMock.Setup(x => x.Save(It.IsAny<DeskBooking>())) // access invocation arguments when returning a value
        .Callback<DeskBooking>(deskBooking =>
        {
          savedDeskBooking = deskBooking;//when save m() is called
        });

     var res= _service.BookDesk(_request);

       //Verify if called at least once  
      _deskBookingRepositoryMock.Verify(x => x.Save(It.IsAny<DeskBooking>()), Times.Once);

      Assert.NotNull(savedDeskBooking);
      Assert.Equal(_request.FirstName, savedDeskBooking.FirstName);
      Assert.Equal(_request.LastName, savedDeskBooking.LastName);
      Assert.Equal(_request.Email, savedDeskBooking.Email);
      Assert.Equal(_request.Date, savedDeskBooking.Date);
      Assert.Equal(_availableDesks.First().Id, savedDeskBooking.DeskId);
    }

    [Fact]
    public void ShouldNotSaveDeskBookingIfNoDeskIsAvailable()
    {
      _availableDesks.Clear();

      _service.BookDesk(_request);

      _deskBookingRepositoryMock.Verify(x => x.Save(It.IsAny<DeskBooking>()), Times.Never);
    }

    [Theory]
    [InlineData(DeskBookingResultCode.Success, true)]
    [InlineData(DeskBookingResultCode.NoDeskAvailable, false)]
    public void ShouldReturnExpectedResultCode(
      DeskBookingResultCode expectedResultCode, bool isDeskAvailable)
    {
      if (!isDeskAvailable)
      {
        _availableDesks.Clear();
      }

      var result = _service.BookDesk(_request);

      Assert.Equal(expectedResultCode, result.Code);
    }

    [Theory]
    [InlineData(5, true)]
    [InlineData(null, false)]
    public void ShouldReturnExpectedDeskBookingId(
      int? expectedDeskBookingId, bool isDeskAvailable)
    {
      if (!isDeskAvailable)
      {
        _availableDesks.Clear();
      }
      else
      {
        _deskBookingRepositoryMock.Setup(x => x.Save(It.IsAny<DeskBooking>()))
          .Callback<DeskBooking>(deskBooking =>
          {
            deskBooking.Id = expectedDeskBookingId.Value;
          });
      }

      var result = _service.BookDesk(_request);

      Assert.Equal(expectedDeskBookingId, result.DeskBookingId);
    }
  }
}
