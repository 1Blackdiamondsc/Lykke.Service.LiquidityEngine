using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Logs;
using Lykke.Service.LiquidityEngine.Domain;
using Lykke.Service.LiquidityEngine.Domain.MarketMaker;
using Lykke.Service.LiquidityEngine.Domain.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Lykke.Service.LiquidityEngine.DomainServices.Tests
{
    [TestClass]
    public class HedgeServiceTests
    {
        private readonly Mock<IPositionService> _positionServiceMock =
            new Mock<IPositionService>();

        private readonly Mock<IInstrumentService> _instrumentServiceMock =
            new Mock<IInstrumentService>();

        private readonly Mock<IExternalExchangeService> _externalExchangeServiceMock =
            new Mock<IExternalExchangeService>();

        private readonly Mock<IMarketMakerStateService> _marketMakerStateServiceMock =
            new Mock<IMarketMakerStateService>();

        private readonly Mock<IRemainingVolumeService> _remainingVolumeServiceMock =
            new Mock<IRemainingVolumeService>();

        private readonly List<Position> _openPositions = new List<Position>();

        private readonly List<Instrument> _instruments = new List<Instrument>();
        
        private readonly List<RemainingVolume> _remainingVolumes = new List<RemainingVolume>();

        private readonly MarketMakerState _marketMakerState = new MarketMakerState {Status = MarketMakerStatus.Active};
        
        private HedgeService _service;

        [TestInitialize]
        public void TestInitialize()
        {
            _positionServiceMock.Setup(o => o.GetOpenAllAsync())
                .Returns(() => Task.FromResult<IReadOnlyCollection<Position>>(_openPositions));

            _marketMakerStateServiceMock.Setup(o => o.GetStateAsync())
                .Returns(() => Task.FromResult(_marketMakerState));

            _instrumentServiceMock.Setup(o => o.GetByAssetPairIdAsync(It.IsAny<string>()))
                .Returns((string assetPairId) =>
                    Task.FromResult(_instruments.SingleOrDefault(o => o.AssetPairId == assetPairId)));

            _remainingVolumeServiceMock.Setup(o => o.GetAllAsync())
                .Returns(() => Task.FromResult<IReadOnlyCollection<RemainingVolume>>(_remainingVolumes));
            
            _service = new HedgeService(
                _positionServiceMock.Object,
                _instrumentServiceMock.Object,
                _externalExchangeServiceMock.Object,
                _marketMakerStateServiceMock.Object,
                _remainingVolumeServiceMock.Object,
                EmptyLogFactory.Instance);
        }

        [TestMethod]
        public async Task Close_Long_Position_Create_Remaining_Volume()
        {
            // arrange

            var position = Position.Open(new[]
            {
                new InternalTrade
                {
                    AssetPairId = "BTCUSD",
                    Type = TradeType.Buy,
                    Price = 6500,
                    Volume = 1.12345m
                }
            });

            var instrument = new Instrument
            {
                AssetPairId = "BTCUSD",
                Mode = InstrumentMode.Active,
                MinVolume = 0.0001m,
                VolumeAccuracy = 3
            };
            
            var expectedRemainingVolume = new RemainingVolume
            {
                AssetPairId = "BTCUSD",
                Volume = position.Volume - Math.Round(position.Volume, instrument.VolumeAccuracy)
            };

            RemainingVolume actualRemainingVolume = null;
            
            decimal externalTradePrice = 6505;
            
            _openPositions.Add(position);

            _instruments.Add(instrument);

            _externalExchangeServiceMock
                .Setup(o => o.ExecuteSellLimitOrderAsync(It.IsAny<string>(), It.IsAny<decimal>()))
                .Returns((string assetPairId, decimal volume) => Task.FromResult(new ExternalTrade
                {
                    AssetPairId = assetPairId,
                    Price = externalTradePrice,
                    Volume = volume,
                    Type = TradeType.Sell
                }));

            _remainingVolumeServiceMock.Setup(o => o.RegisterVolumeAsync(It.IsAny<string>(), It.IsAny<decimal>()))
                .Returns(Task.CompletedTask)
                .Callback((string assetPairId, decimal volume) => actualRemainingVolume =
                    new RemainingVolume {AssetPairId = assetPairId, Volume = volume});
            
            // act

            await _service.ExecuteAsync();

            // assert
            
            Assert.IsTrue(AreEqual(expectedRemainingVolume, actualRemainingVolume));
        }
        
        [TestMethod]
        public async Task Close_Short_Position_Create_Remaining_Volume()
        {
            // arrange

            var position = Position.Open(new[]
            {
                new InternalTrade
                {
                    AssetPairId = "BTCUSD",
                    Type = TradeType.Sell,
                    Price = 6500,
                    Volume = 1.12345m
                }
            });

            var instrument = new Instrument
            {
                AssetPairId = "BTCUSD",
                Mode = InstrumentMode.Active,
                MinVolume = 0.0001m,
                VolumeAccuracy = 3
            };
            
            var expectedRemainingVolume = new RemainingVolume
            {
                AssetPairId = "BTCUSD",
                Volume = -(position.Volume - Math.Round(position.Volume, instrument.VolumeAccuracy))
            };

            RemainingVolume actualRemainingVolume = null;
            
            decimal externalTradePrice = 6505;
            
            _openPositions.Add(position);

            _instruments.Add(instrument);

            _externalExchangeServiceMock
                .Setup(o => o.ExecuteBuyLimitOrderAsync(It.IsAny<string>(), It.IsAny<decimal>()))
                .Returns((string assetPairId, decimal volume) => Task.FromResult(new ExternalTrade
                {
                    AssetPairId = assetPairId,
                    Price = externalTradePrice,
                    Volume = volume,
                    Type = TradeType.Buy
                }));

            _remainingVolumeServiceMock.Setup(o => o.RegisterVolumeAsync(It.IsAny<string>(), It.IsAny<decimal>()))
                .Returns(Task.CompletedTask)
                .Callback((string assetPairId, decimal volume) => actualRemainingVolume =
                    new RemainingVolume {AssetPairId = assetPairId, Volume = volume});
            
            // act

            await _service.ExecuteAsync();

            // assert
            
            Assert.IsTrue(AreEqual(expectedRemainingVolume, actualRemainingVolume));
        }

        private bool AreEqual(RemainingVolume a, RemainingVolume b)
        {
            return a.AssetPairId == b.AssetPairId && a.Volume == b.Volume;
        }
    }
}
