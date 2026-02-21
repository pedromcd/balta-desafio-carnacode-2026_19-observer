using System;
using System.Collections.Generic;
using System.Threading;

namespace DesignPatternChallenge
{
    // ============================
    // EVENTO / DADOS DA NOTIFICAÇÃO
    // ============================
    public class StockPriceChanged
    {
        public string Symbol { get; }
        public decimal OldPrice { get; }
        public decimal NewPrice { get; }
        public decimal ChangePercent { get; }
        public DateTime Timestamp { get; }

        public StockPriceChanged(string symbol, decimal oldPrice, decimal newPrice, DateTime timestamp)
        {
            Symbol = symbol;
            OldPrice = oldPrice;
            NewPrice = newPrice;
            Timestamp = timestamp;

            // Proteção: evita divisão por zero
            ChangePercent = oldPrice == 0 ? 0 : ((newPrice - oldPrice) / oldPrice) * 100m;
        }
    }

    // ============================
    // OBSERVER (ASSINANTE)
    // ============================
    public interface IStockObserver
    {
        void OnPriceChanged(StockPriceChanged evt);
    }

    // ============================
    // SUBJECT (PUBLICADOR)
    // ============================
    public class Stock
    {
        public string Symbol { get; }
        public decimal Price { get; private set; }
        public DateTime LastUpdate { get; private set; }

        private readonly List<IStockObserver> _observers = new List<IStockObserver>();

        public Stock(string symbol, decimal initialPrice)
        {
            Symbol = symbol;
            Price = initialPrice;
            LastUpdate = DateTime.Now;
        }

        public void Subscribe(IStockObserver observer)
        {
            if (observer == null) return;
            if (_observers.Contains(observer)) return;

            _observers.Add(observer);
            Console.WriteLine($"[{Symbol}] ✅ Novo assinante: {observer.GetType().Name}");
        }

        public void Unsubscribe(IStockObserver observer)
        {
            if (observer == null) return;

            if (_observers.Remove(observer))
            {
                Console.WriteLine($"[{Symbol}] ❎ Assinante removido: {observer.GetType().Name}");
            }
        }

        public void UpdatePrice(decimal newPrice)
        {
            if (Price == newPrice) return;

            var oldPrice = Price;
            Price = newPrice;
            LastUpdate = DateTime.Now;

            var evt = new StockPriceChanged(Symbol, oldPrice, newPrice, LastUpdate);

            Console.WriteLine($"\n[{Symbol}] Preço atualizado: R$ {oldPrice:N2} → R$ {newPrice:N2} ({evt.ChangePercent:+0.00;-0.00}%)");

            NotifyAll(evt);
        }

        private void NotifyAll(StockPriceChanged evt)
        {
            // Faz uma cópia pra evitar erro caso alguém se desinscreva durante a notificação
            var snapshot = _observers.ToArray();

            foreach (var observer in snapshot)
            {
                try
                {
                    observer.OnPriceChanged(evt);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  → [Erro ao notificar {observer.GetType().Name}] {ex.Message}");
                }
            }
        }
    }

    // ============================
    // OBSERVERS CONCRETOS
    // ============================
    public class Investor : IStockObserver
    {
        public string Name { get; }
        public decimal AlertThreshold { get; } // em %

        public Investor(string name, decimal alertThreshold)
        {
            Name = name;
            AlertThreshold = alertThreshold;
        }

        public void OnPriceChanged(StockPriceChanged evt)
        {
            Console.WriteLine($"  → [Investidor {Name}] Notificado sobre {evt.Symbol}");

            if (Math.Abs(evt.ChangePercent) >= AlertThreshold)
            {
                Console.WriteLine($"  → [Investidor {Name}] ⚠️ ALERTA! Mudança de {evt.ChangePercent:+0.00;-0.00}% excedeu limite de {AlertThreshold}%");
            }
        }
    }

    public class MobileApp : IStockObserver
    {
        public string UserId { get; }

        public MobileApp(string userId)
        {
            UserId = userId;
        }

        public void OnPriceChanged(StockPriceChanged evt)
        {
            Console.WriteLine($"  → [App Mobile {UserId}] 📱 Push: {evt.Symbol} agora em R$ {evt.NewPrice:N2} ({evt.ChangePercent:+0.00;-0.00}%)");
        }
    }

    public class TradingBot : IStockObserver
    {
        public string BotName { get; }
        public decimal BuyThreshold { get; }  // compra quando cai X%
        public decimal SellThreshold { get; } // vende quando sobe X%

        public TradingBot(string botName, decimal buyThreshold, decimal sellThreshold)
        {
            BotName = botName;
            BuyThreshold = buyThreshold;
            SellThreshold = sellThreshold;
        }

        public void OnPriceChanged(StockPriceChanged evt)
        {
            Console.WriteLine($"  → [Bot {BotName}] 🤖 Analisando {evt.Symbol}...");

            if (evt.ChangePercent <= -BuyThreshold)
            {
                Console.WriteLine($"  → [Bot {BotName}] 💰 COMPRANDO {evt.Symbol} por R$ {evt.NewPrice:N2}");
            }
            else if (evt.ChangePercent >= SellThreshold)
            {
                Console.WriteLine($"  → [Bot {BotName}] 💸 VENDENDO {evt.Symbol} por R$ {evt.NewPrice:N2}");
            }
        }
    }

    // Opcional: logger central (exemplo de “novo observador” sem mexer em Stock)
    public class AuditLogger : IStockObserver
    {
        public void OnPriceChanged(StockPriceChanged evt)
        {
            Console.WriteLine($"  → [Audit] {evt.Timestamp:HH:mm:ss} {evt.Symbol}: {evt.OldPrice:N2} → {evt.NewPrice:N2}");
        }
    }

    // ============================
    // DEMO
    // ============================
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Sistema de Monitoramento de Ações (Observer) ===");

            var petr4 = new Stock("PETR4", 35.50m);

            var investor1 = new Investor("João Silva", 3.0m);
            var investor2 = new Investor("Maria Santos", 5.0m);
            var investor3 = new Investor("Pedro Marques", 2.0m); // agora dá pra ter quantos quiser

            var mobileApp = new MobileApp("user123");
            var tradingBot = new TradingBot("AlgoTrader", 2.0m, 2.5m);
            var audit = new AuditLogger();

            // ✅ Um único método para qualquer tipo de observador
            petr4.Subscribe(investor1);
            petr4.Subscribe(investor2);
            petr4.Subscribe(investor3);
            petr4.Subscribe(mobileApp);
            petr4.Subscribe(tradingBot);
            petr4.Subscribe(audit);

            Console.WriteLine("\n=== Movimentações do Mercado ===");

            petr4.UpdatePrice(36.20m); // +1.97%
            Thread.Sleep(300);

            petr4.UpdatePrice(37.50m); // +3.59%
            Thread.Sleep(300);

            // ✅ Remoção dinâmica
            Console.WriteLine("\n=== Removendo um observador (Investor2) ===");
            petr4.Unsubscribe(investor2);

            petr4.UpdatePrice(35.00m); // -6.67%
            Thread.Sleep(300);

            Console.WriteLine("\n=== O que foi resolvido ===");
            Console.WriteLine("✓ Stock não conhece Investor/MobileApp/TradingBot");
            Console.WriteLine("✓ Adicionar observador novo não exige alterar Stock (OCP ok)");
            Console.WriteLine("✓ Suporta N observadores do mesmo tipo");
            Console.WriteLine("✓ Subscribe/Unsubscribe dinâmicos");
            Console.WriteLine("✓ Sem polling (event-driven)");
        }
    }
}