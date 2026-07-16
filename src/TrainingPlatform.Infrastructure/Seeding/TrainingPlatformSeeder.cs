using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TrainingPlatform.Domain.Challenges;
using TrainingPlatform.Domain.Common.Enumerations;
using TrainingPlatform.Domain.Questions;
using TrainingPlatform.Domain.Topics;
using TrainingPlatform.Infrastructure.Persistence;

namespace TrainingPlatform.Infrastructure.Seeding;

public static class TrainingPlatformSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TrainingPlatformDbContext>();

        if (dbContext.Database.IsNpgsql())
        {
            // Real deployments evolve the schema through migrations. Databases
            // created before migrations existed must be baselined once — see
            // docker/baseline-migrations.sql.
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            // Test hosts run on Sqlite, which cannot execute Npgsql-generated
            // migrations; EnsureCreated builds the schema straight from the model.
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        }

        var now = DateTime.UtcNow;

        if (!await dbContext.Topics.AnyAsync(cancellationToken))
        {
            await SeedTopicsAsync(dbContext, now, cancellationToken);
        }

        // Content pools top up on every startup so databases created before new
        // pool content was authored pick it up automatically. Existing rows are
        // matched by (topic, prompt/title), which makes the passes idempotent.
        await TopUpQuestionPoolAsync(dbContext, now, cancellationToken);
        await TopUpChallengePoolAsync(dbContext, now, cancellationToken);
    }

    private static async Task SeedTopicsAsync(TrainingPlatformDbContext dbContext, DateTime now, CancellationToken cancellationToken)
    {
        var csharp = Topic.Create("C# Temelleri", "csharp-foundations", "Dil temelleri, LINQ ve asenkron desenler.", TopicDifficulty.Fundamental, 1.2d, [], now);
        var aspNet = Topic.Create("ASP.NET Core API Tasarımı", "aspnet-core-api-design", "Controller'lar, sözleşmeler, kimlik doğrulama ve HTTP konuları.", TopicDifficulty.Intermediate, 1.15d, [csharp.Id], now);
        var efCore = Topic.Create("EF Core", "ef-core", "Kalıcılık, eşlemeler ve sorgu davranışı.", TopicDifficulty.Intermediate, 1.2d, [csharp.Id], now);
        var cleanArchitecture = Topic.Create("Temiz Mimari", "clean-architecture", "Sınırlar, kullanım senaryoları ve bağımlılık kuralları.", TopicDifficulty.Advanced, 1.05d, [aspNet.Id, efCore.Id], now);
        var cqrs = Topic.Create("CQRS", "cqrs", "Komutlar, sorgular ve davranışsal ayrım.", TopicDifficulty.Advanced, 1.1d, [cleanArchitecture.Id], now);
        var jwt = Topic.Create("JWT Kimlik Doğrulama", "jwt-authentication", "Token üretimi, doğrulama ve claim tasarımı.", TopicDifficulty.Intermediate, 1.1d, [aspNet.Id], now);
        var caching = Topic.Create("Önbellekleme Stratejisi", "caching-strategy", "Okuma optimizasyonu, geçersiz kılma ve ödünleşimler.", TopicDifficulty.Advanced, 1.0d, [aspNet.Id, efCore.Id], now);
        var postgres = Topic.Create("PostgreSQL", "postgresql", "İndeksler, transaction'lar ve ilişkisel modelleme.", TopicDifficulty.Intermediate, 1.15d, [efCore.Id], now);

        await dbContext.Topics.AddRangeAsync([csharp, aspNet, efCore, cleanArchitecture, cqrs, jwt, caching, postgres], cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task TopUpChallengePoolAsync(TrainingPlatformDbContext dbContext, DateTime now, CancellationToken cancellationToken)
    {
        var topicIdsBySlug = await dbContext.Topics
            .ToDictionaryAsync(topic => topic.Slug, topic => topic.Id, cancellationToken);

        var existingCoding = (await dbContext.CodingChallenges
            .Select(challenge => new { challenge.TopicId, challenge.Title })
            .ToListAsync(cancellationToken))
            .Select(entry => (entry.TopicId, entry.Title))
            .ToHashSet();

        var existingScenario = (await dbContext.ScenarioChallenges
            .Select(challenge => new { challenge.TopicId, challenge.Title })
            .ToListAsync(cancellationToken))
            .Select(entry => (entry.TopicId, entry.Title))
            .ToHashSet();

        var added = false;

        foreach (var spec in CodingChallengePool())
        {
            if (!topicIdsBySlug.TryGetValue(spec.TopicSlug, out var topicId) || existingCoding.Contains((topicId, spec.Title)))
            {
                continue;
            }

            dbContext.CodingChallenges.Add(CodingChallenge.Create(
                topicId, spec.Title, spec.Description, spec.Difficulty, spec.Minutes,
                spec.Criteria, spec.StarterCode, spec.ExpectedOutcome, now, spec.TestCode));
            added = true;
        }

        foreach (var spec in ScenarioChallengePool())
        {
            if (!topicIdsBySlug.TryGetValue(spec.TopicSlug, out var topicId) || existingScenario.Contains((topicId, spec.Title)))
            {
                continue;
            }

            dbContext.ScenarioChallenges.Add(ScenarioChallenge.Create(
                topicId, spec.Title, spec.Scenario, spec.Difficulty, spec.Minutes,
                spec.Criteria, spec.ReferenceSolution, now));
            added = true;
        }

        if (added)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task TopUpQuestionPoolAsync(TrainingPlatformDbContext dbContext, DateTime now, CancellationToken cancellationToken)
    {
        var topicIdsBySlug = await dbContext.Topics
            .ToDictionaryAsync(topic => topic.Slug, topic => topic.Id, cancellationToken);

        var existingQuestions = await dbContext.Questions
            .Select(question => new { question.TopicId, question.Prompt })
            .ToListAsync(cancellationToken);
        var existingLookup = existingQuestions
            .Select(entry => (entry.TopicId, entry.Prompt))
            .ToHashSet();

        var added = false;
        foreach (var spec in QuestionPool())
        {
            if (!topicIdsBySlug.TryGetValue(spec.TopicSlug, out var topicId))
            {
                continue;
            }

            if (existingLookup.Contains((topicId, spec.Prompt)))
            {
                continue;
            }

            dbContext.Questions.Add(Question.Create(
                topicId,
                spec.Type,
                spec.Prompt,
                spec.Explanation,
                spec.Difficulty,
                spec.Seconds,
                spec.PassScore,
                spec.Tags,
                spec.Accepted,
                spec.Options,
                now));
            added = true;
        }

        if (added)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private sealed record CodingChallengeSpec(
        string TopicSlug,
        string Title,
        string Description,
        TopicDifficulty Difficulty,
        int Minutes,
        string[] Criteria,
        string StarterCode,
        string ExpectedOutcome,
        string TestCode);

    private sealed record ScenarioChallengeSpec(
        string TopicSlug,
        string Title,
        string Scenario,
        TopicDifficulty Difficulty,
        int Minutes,
        string[] Criteria,
        string ReferenceSolution);

    /// <summary>
    /// Coding challenge pool. Challenges with TestCode are compiled and executed
    /// against the learner's submission by the judge runner; ones without stay
    /// on the review/AI-feedback path. Titles are identity — never reword one
    /// in place, add a new entry instead.
    /// </summary>
    private static IEnumerable<CodingChallengeSpec> CodingChallengePool()
    {
        yield return new CodingChallengeSpec(
            "cqrs",
            "Konu Ustalığı için Query Handler Ekle",
            "EF varlıklarını API katmanına sızdırmadan konu ustalık metriklerini döndüren bir CQRS sorgusu tasarla.",
            TopicDifficulty.Advanced, 60,
            ["Uygulama katmanı DTO'su kullan", "Altyapı kaygılarını API dışında tut", "Konuya göre filtrelemeyi destekle"],
            "public sealed record GetTopicMasteryQuery(Guid UserId);",
            "Ustalık ayrıntılarını temiz biçimde döndüren bir query handler ve yanıt DTO'su.",
            TestCode: "");

        yield return new CodingChallengeSpec(
            "aspnet-core-api-design",
            "Bir Eğitim Endpoint'ini JWT ile Güvenceye Al",
            "Bir endpoint'e JWT koruması ekle ve mevcut kullanıcı kimliğini claim'lerden çıkar.",
            TopicDifficulty.Intermediate, 45,
            ["Authorize kullan", "Claim erişimini doğrula", "İstek gövdesindeki kullanıcı kimliğine güvenme"],
            "[Authorize]\npublic sealed class StudyPlansController : ControllerBase { }",
            "Kullanıcı kimliğini kimliği doğrulanmış principal'dan okuyan korumalı bir endpoint.",
            TestCode: "");

        yield return new CodingChallengeSpec(
            "csharp-foundations",
            "Kayan pencere (sliding window) hız sınırlayıcı yaz",
            """
            En fazla `limit` çağrıya kayan bir zaman penceresi içinde izin veren bir
            hız sınırlayıcı (rate limiter) yaz. T anındaki bir çağrıya, (T - window, T]
            aralığında `limit`ten AZ çağrıya İZİN VERİLMİŞSE izin verilir. Reddedilen
            çağrılar kapasite tüketmez.

            Test paketinin sınıfı bulabilmesi için sınıfı global namespace'te tut
            (namespace bildirimi olmadan). Saat dışarıdan verilir — sınıfın içinde
            DateTime.UtcNow kullanma.
            """,
            TopicDifficulty.Intermediate, 30,
            ["Kayan pencere semantiği (sabit kova değil)", "Reddedilen çağrılar kapasite tüketmez", "Sınıf içinde duvar saatine erişim yok"],
            """
            public class SlidingWindowRateLimiter
            {
                public SlidingWindowRateLimiter(int limit, TimeSpan window)
                {
                    // TODO
                }

                // 'nowUtc' anındaki çağrıya izin veriliyorsa true, pencere zaten
                // 'limit' kadar izinli çağrı içeriyorsa false döner.
                public bool TryAcquire(DateTime nowUtc)
                {
                    // TODO
                    return false;
                }
            }
            """,
            "Tüm rate-limiter testleri yeşil: kapasiteye uyuluyor, pencere kayıyor, reddedilenler kapasite tüketmiyor.",
            """
            using Xunit;

            public class SlidingWindowRateLimiterTests
            {
                private static readonly DateTime T0 = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

                [Fact]
                public void Allows_calls_up_to_the_limit()
                {
                    var limiter = new SlidingWindowRateLimiter(3, TimeSpan.FromSeconds(10));
                    Assert.True(limiter.TryAcquire(T0));
                    Assert.True(limiter.TryAcquire(T0.AddSeconds(1)));
                    Assert.True(limiter.TryAcquire(T0.AddSeconds(2)));
                }

                [Fact]
                public void Rejects_the_call_over_the_limit()
                {
                    var limiter = new SlidingWindowRateLimiter(2, TimeSpan.FromSeconds(10));
                    Assert.True(limiter.TryAcquire(T0));
                    Assert.True(limiter.TryAcquire(T0.AddSeconds(1)));
                    Assert.False(limiter.TryAcquire(T0.AddSeconds(2)));
                }

                [Fact]
                public void Allows_again_after_the_window_slides()
                {
                    var limiter = new SlidingWindowRateLimiter(2, TimeSpan.FromSeconds(10));
                    Assert.True(limiter.TryAcquire(T0));
                    Assert.True(limiter.TryAcquire(T0.AddSeconds(1)));
                    Assert.False(limiter.TryAcquire(T0.AddSeconds(5)));
                    Assert.True(limiter.TryAcquire(T0.AddSeconds(10.5)));
                }

                [Fact]
                public void Rejected_calls_do_not_consume_capacity()
                {
                    var limiter = new SlidingWindowRateLimiter(1, TimeSpan.FromSeconds(10));
                    Assert.True(limiter.TryAcquire(T0));
                    Assert.False(limiter.TryAcquire(T0.AddSeconds(1)));
                    Assert.False(limiter.TryAcquire(T0.AddSeconds(2)));
                    Assert.True(limiter.TryAcquire(T0.AddSeconds(11)));
                }
            }
            """);

        yield return new CodingChallengeSpec(
            "caching-strategy",
            "Bellek içi LRU önbellek yaz",
            """
            Sabit kapasiteli bir en-az-son-kullanılan (LRU) önbellek yaz. Önbellek
            doluyken yeni bir anahtar eklemek, en uzun süredir kullanılmayan girdiyi
            tahliye eder. Hem okuma (TryGet) hem de yazma (Set) bir girdinin
            tazeliğini yeniler.

            Test paketinin sınıfı bulabilmesi için sınıfı global namespace'te tut.
            """,
            TopicDifficulty.Advanced, 40,
            ["Hedef O(1) get/set", "Okumalar tazeliği yeniler", "Var olan anahtarı güncellemek önbelleği büyütmemeli"],
            """
            public class LruCache<TKey, TValue> where TKey : notnull
            {
                public LruCache(int capacity)
                {
                    // TODO
                }

                public int Count => 0; // TODO

                public bool TryGet(TKey key, out TValue? value)
                {
                    // TODO
                    value = default;
                    return false;
                }

                public void Set(TKey key, TValue value)
                {
                    // TODO
                }
            }
            """,
            "Tüm LRU testleri yeşil: kapasite tahliyesi, okumada tazelik yenileme, yerinde güncelleme.",
            """
            using Xunit;

            public class LruCacheTests
            {
                [Fact]
                public void Stores_and_retrieves_values()
                {
                    var cache = new LruCache<string, int>(2);
                    cache.Set("a", 1);
                    Assert.True(cache.TryGet("a", out var value));
                    Assert.Equal(1, value);
                }

                [Fact]
                public void Evicts_the_least_recently_used_entry_at_capacity()
                {
                    var cache = new LruCache<string, int>(2);
                    cache.Set("a", 1);
                    cache.Set("b", 2);
                    cache.Set("c", 3);
                    Assert.False(cache.TryGet("a", out _));
                    Assert.True(cache.TryGet("b", out _));
                    Assert.True(cache.TryGet("c", out _));
                    Assert.Equal(2, cache.Count);
                }

                [Fact]
                public void Reading_an_entry_refreshes_its_recency()
                {
                    var cache = new LruCache<string, int>(2);
                    cache.Set("a", 1);
                    cache.Set("b", 2);
                    Assert.True(cache.TryGet("a", out _));
                    cache.Set("c", 3);
                    Assert.True(cache.TryGet("a", out _));
                    Assert.False(cache.TryGet("b", out _));
                }

                [Fact]
                public void Updating_an_existing_key_replaces_the_value_without_growing()
                {
                    var cache = new LruCache<string, int>(2);
                    cache.Set("a", 1);
                    cache.Set("a", 10);
                    Assert.Equal(1, cache.Count);
                    Assert.True(cache.TryGet("a", out var value));
                    Assert.Equal(10, value);
                }
            }
            """);

        yield return new CodingChallengeSpec(
            "aspnet-core-api-design",
            "Bir liste endpoint'i için sayfalama meta verisi yaz",
            """
            Bir kaynak listesini tek bir sayfaya böl ve bir liste endpoint'inin
            döndüreceği sayfalama meta verisini raporla. İstenen sayfayı geçerli
            aralığa sıkıştır: 1'in altı 1 olur; son sayfanın ötesi son sayfa olur.
            Boş bir kaynak, sıfır toplamlı boş bir ilk sayfa döndürür.

            Test paketinin bulabilmesi için her iki tipi de global namespace'te tut.
            """,
            TopicDifficulty.Intermediate, 25,
            ["İstenen sayfa için doğru dilim", "Sayfa numarası geçerli aralığa sıkıştırılmış", "Doğru TotalCount/TotalPages meta verisi"],
            """
            public sealed record PagedResult<T>(
                IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount, int TotalPages);

            public static class Paginator
            {
                public static PagedResult<T> Paginate<T>(IReadOnlyList<T> source, int page, int pageSize)
                {
                    // TODO
                    return new PagedResult<T>(Array.Empty<T>(), 1, pageSize, 0, 0);
                }
            }
            """,
            "Tüm sayfalama testleri yeşil: dilimleme, sıkıştırma ve meta veri.",
            """
            using Xunit;

            public class PaginatorTests
            {
                private static readonly IReadOnlyList<int> Source = Enumerable.Range(1, 25).ToList();

                [Fact]
                public void Returns_the_requested_page_slice()
                {
                    var page = Paginator.Paginate(Source, page: 2, pageSize: 10);
                    Assert.Equal(new[] { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 }, page.Items);
                    Assert.Equal(2, page.Page);
                    Assert.Equal(25, page.TotalCount);
                    Assert.Equal(3, page.TotalPages);
                }

                [Fact]
                public void Last_page_may_be_partial()
                {
                    var page = Paginator.Paginate(Source, page: 3, pageSize: 10);
                    Assert.Equal(new[] { 21, 22, 23, 24, 25 }, page.Items);
                }

                [Fact]
                public void Page_below_one_clamps_to_the_first_page()
                {
                    var page = Paginator.Paginate(Source, page: 0, pageSize: 10);
                    Assert.Equal(1, page.Page);
                    Assert.Equal(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, page.Items);
                }

                [Fact]
                public void Page_past_the_end_clamps_to_the_last_page()
                {
                    var page = Paginator.Paginate(Source, page: 99, pageSize: 10);
                    Assert.Equal(3, page.Page);
                    Assert.Equal(new[] { 21, 22, 23, 24, 25 }, page.Items);
                }

                [Fact]
                public void Empty_source_yields_an_empty_first_page()
                {
                    var page = Paginator.Paginate(new List<int>(), page: 1, pageSize: 10);
                    Assert.Empty(page.Items);
                    Assert.Equal(1, page.Page);
                    Assert.Equal(0, page.TotalCount);
                    Assert.Equal(0, page.TotalPages);
                }
            }
            """);
    }

    private static IEnumerable<ScenarioChallengeSpec> ScenarioChallengePool()
    {
        yield return new ScenarioChallengeSpec(
            "clean-architecture",
            "Modüler Monolit Sınır İncelemesi",
            "Analitik, tekrar planlama ve yapay zeka geri bildirimini tek bir dağıtılabilir uygulamada barındırman gerekiyor. Bugün modül sınırlarının nerede olması gerektiğini ve daha sonra neyin ayrılacağını açıkla.",
            TopicDifficulty.Advanced, 45,
            ["sınır netliği", "veri sahipliği", "ileride ayırma"],
            "Modülleri önce uygulama/alan (domain) sınırlarıyla ayır; servisleri ancak ekip ve dağıtım baskısı gerektirdiğinde çıkar.");

        yield return new ScenarioChallengeSpec(
            "caching-strategy",
            "Panel Okumalarını Önbelleğe Alma",
            "Günlük bir panel; kullanıcı ilerlemesini, planları ve soru geçmişini birleştirdiği için yavaş. Burada önbelleklemenin yeri var mı ve geçersiz kılma nasıl çalışmalı, açıkla.",
            TopicDifficulty.Advanced, 45,
            ["önbellek kapsamı", "tazelik", "geçersiz kılma"],
            "Okuma modellerini önbelleğe al; cevap gönderiminde ve plan üretiminde açıkça geçersiz kıl; yazmaları veya doğruluk kaynağı durumu önbelleğe alma.");
    }

    private sealed record QuestionSpec(
        string TopicSlug,
        QuestionType Type,
        TopicDifficulty Difficulty,
        string Prompt,
        string Explanation,
        int Seconds,
        int PassScore,
        string[] Tags,
        string[] Accepted,
        (string Text, bool IsCorrect)[] Options);

    private static QuestionSpec Mc(string slug, TopicDifficulty difficulty, string prompt, string explanation, int seconds, string[] tags, params (string Text, bool IsCorrect)[] options)
        => new(slug, QuestionType.MultipleChoice, difficulty, prompt, explanation, seconds, 100, tags, [], options);

    private static QuestionSpec Sa(string slug, TopicDifficulty difficulty, string prompt, string explanation, int seconds, string[] tags, params string[] accepted)
        => new(slug, QuestionType.ShortAnswer, difficulty, prompt, explanation, seconds, 70, tags, accepted, []);

    private static QuestionSpec Sc(string slug, TopicDifficulty difficulty, string prompt, string explanation, int seconds, string[] keywords)
        => new(slug, QuestionType.Scenario, difficulty, prompt, explanation, seconds, 60, ["scenario"], keywords, []);

    /// <summary>
    /// The per-topic question pool. Daily plans draw a rotating subset from here,
    /// so each topic should keep roughly 8+ questions across difficulty levels.
    /// Prompts double as identity — never reword an existing prompt in place,
    /// add a new entry instead (the top-up pass dedupes by topic + prompt).
    /// </summary>
    private static IEnumerable<QuestionSpec> QuestionPool()
    {
        const string csharp = "csharp-foundations";
        const string aspnet = "aspnet-core-api-design";
        const string ef = "ef-core";
        const string clean = "clean-architecture";
        const string cqrs = "cqrs";
        const string jwt = "jwt-authentication";
        const string caching = "caching-strategy";
        const string pg = "postgresql";

        // ── C# Temelleri ──────────────────────────────────────────────────
        yield return Mc(csharp, TopicDifficulty.Fundamental,
            "Hangi LINQ operatörü her elemanı yeni bir biçime dönüştürür (projeksiyon)?",
            "Select her girdi elemanını yeni bir şekle dönüştürür.",
            60, ["linq"], ("Where", false), ("Select", true), ("GroupBy", false));
        yield return Sa(csharp, TopicDifficulty.Fundamental,
            "C#'ta bir Task sonucunu asenkron olarak beklemek için hangi anahtar kelime kullanılır?",
            "await anahtar kelimesi, görev tamamlandığında asenkron olarak yürütmeyi sürdürür.",
            45, ["async"], "await");
        yield return Mc(csharp, TopicDifficulty.Fundamental,
            "Aşağıdaki C# tür bildirimlerinden hangisi bir değer türü (value type) üretir?",
            "Struct'lar (record struct dahil) stack'te veya kapsayıcıların içinde satır içi tutulur ve değerle kopyalanır; class, interface ve delegate referans türleridir.",
            60, ["types"], ("class Order", false), ("record struct Money(decimal Amount)", true), ("interface IOrder", false), ("delegate void Handler()", false));
        yield return Mc(csharp, TopicDifficulty.Intermediate,
            "`if (value is { } x)` property (özellik) deseni neyi kontrol eder?",
            "`is { }` null olmayan herhangi bir değeri eşleştirir ve null olamayan bir değişken tanımlar — özlü bir null kontrolü artı dönüştürmedir.",
            75, ["pattern-matching"], ("value'nun null olduğunu", false), ("value'nun null olmadığını ve onu x'e bağladığını", true), ("value'nun boş bir koleksiyon olduğunu", false), ("value'nun varsayılan bir kurucusu olduğunu", false));
        yield return Mc(csharp, TopicDifficulty.Intermediate,
            "Aynı özellik değerlerine sahip iki record örneği == ile karşılaştırılıyor. Sonuç nedir?",
            "Record'lar değere dayalı eşitlik üretir: == referansları değil, özellik değerlerini karşılaştırır.",
            60, ["records"], ("Her zaman false — farklı referanslardır", false), ("True — record'lar değere göre karşılaştırılır", true), ("Derleme hatası — record'lar == desteklemez", false));
        yield return Sa(csharp, TopicDifficulty.Fundamental,
            "Bir sınıfın kalıtılmasını (inherit) hangi değiştirici (modifier) engeller?",
            "sealed, daha fazla türetmeyi durdurur ve JIT'in çağrıları devirtualize etmesine olanak tanır.",
            30, ["types"], "sealed");
        yield return Sa(csharp, TopicDifficulty.Intermediate,
            "IEnumerable<T> üzerindeki LINQ sorguları, numaralandırılana (enumerate) kadar çalışmaz. Bu davranışın adı nedir?",
            "Ertelenmiş (tembel) yürütme — sorgu, oluşturulduğunda değil, üzerinde iterasyon yapıldığında çalışır.",
            60, ["linq"], "ertelenmiş yürütme", "ertelenmiş", "tembel değerlendirme", "deferred execution", "lazy");
        yield return Sc(csharp, TopicDifficulty.Advanced,
            "Eski bir WinForms uygulaması senin async kütüphaneni .Result ile çağırıyor ve donuyor. Ne olduğunu ve kütüphanenin bunu önlemek için nasıl yazılması gerektiğini açıkla.",
            ".Result üzerinde bloklamak, devam işi (continuation) yakalanan bağlama ihtiyaç duyduğunda deadlock'a yol açar; kütüphaneler ConfigureAwait(false) kullanmalı ve çağıranlar baştan sona async kalmalıdır.",
            180, ["deadlock", "configureawait", "async", "bloklama"]);

        // ── ASP.NET Core API Tasarımı ─────────────────────────────────────
        yield return Mc(aspnet, TopicDifficulty.Intermediate,
            "JWT ile korunan API'lerde authorization'dan önce hangi middleware çalışmalıdır?",
            "Authentication, authorization politikaları denetlemeden önce kullanıcı principal'ını doldurur.",
            75, ["auth", "pipeline"], ("UseAuthorization", false), ("UseAuthentication", true), ("UseCors", false));
        yield return Mc(aspnet, TopicDifficulty.Fundamental,
            "ASP.NET Core, [HttpPost] bir controller action'ındaki karmaşık tür parametresi için varsayılan olarak hangi bağlama (binding) kaynağını kullanır?",
            "[ApiController] ile karmaşık türler JSON istek gövdesinden bağlanır; basit türler route veya query'den bağlanır.",
            60, ["model-binding"], ("Query string", false), ("İstek gövdesi (request body)", true), ("Route değerleri", false), ("HTTP başlıkları", false));
        yield return Mc(aspnet, TopicDifficulty.Fundamental,
            "Bir POST endpoint'i yeni bir kaynak oluşturuyor. Başarıyı en iyi hangi durum kodu belirtir?",
            "201 Created (ideal olarak bir Location başlığıyla) başarılı kaynak oluşturmanın sözleşmesidir; 200 semantiği gizler.",
            45, ["http"], ("200 OK", false), ("201 Created", true), ("204 No Content", false), ("302 Found", false));
        yield return Mc(aspnet, TopicDifficulty.Intermediate,
            "Minimal API'lerde MapGroup hangi sorunu çözer?",
            "MapGroup, tek bir yerde bir grup endpoint'e ortak bir route öneki ve ortak meta veri (filtreler, auth) ekler.",
            75, ["minimal-apis"], ("Endpoint çalıştırmayı paralelleştirir", false), ("İlişkili endpoint'lere ortak bir önek ve paylaşılan filtre/meta veri uygular", true), ("OpenAPI belgeleri üretir", false));
        yield return Sa(aspnet, TopicDifficulty.Fundamental,
            "Bir controller veya action'ı, çağıranın kimliğinin doğrulanmış olmasını gerektirecek şekilde hangi attribute işaretler?",
            "[Authorize], endpoint'i kimlik doğrulama ve yetkilendirme hattının arkasına koyar.",
            30, ["auth"], "authorize", "[authorize]");
        yield return Sa(aspnet, TopicDifficulty.Intermediate,
            "Bir middleware bileşeni, sonraki delegate'i çağırmadan geri döndüğünde buna ne ad verilir?",
            "Kısa devre (short-circuit) — hattın geri kalanı hiç çalışmaz; auth hataları ve statik dosyalar bu şekilde erkenden yanıt verir.",
            60, ["pipeline"], "kısa devre", "kısa devre yapma", "short-circuit", "hattı sonlandırma");
        yield return Sa(aspnet, TopicDifficulty.Advanced,
            "RFC 7807, standartlaştırılmış API hata yanıtları için hangi medya tipini (media type) tanımlar?",
            "application/problem+json — ASP.NET Core'un ProblemDetails'i buna serileştirilir.",
            60, ["http", "errors"], "application/problem+json", "problem+json");
        yield return Sc(aspnet, TopicDifficulty.Advanced,
            "Zorla güncelleyemeyeceğin mobil istemcilerin tükettiği bir JSON sözleşmesindeki bir alanı yeniden adlandırman gerekiyor. Bu değişikliği güvenle nasıl yayınlayacağını anlat.",
            "Sözleşmeyi sürümle (veya her iki şekli de kabul et), eski alanı bir kullanımdan kaldırma penceresi boyunca koru ve zaman çizelgesini duyur — dağıtılmış istemcileri asla yerinde bozma.",
            180, ["sürüm", "sözleşme", "geriye dönük", "kullanımdan kaldır"]);

        // ── EF Core ───────────────────────────────────────────────────────
        yield return Mc(ef, TopicDifficulty.Intermediate,
            "Yalnızca salt-okunur sorgu sonuçlarına ihtiyacın olduğunda hangi EF Core API'si uygundur?",
            "AsNoTracking, salt-okunur senaryolarda change tracker yükünden kaçınır.",
            60, ["ef-core"], ("AsTracking", false), ("AsNoTracking", true), ("Attach", false));
        yield return Mc(ef, TopicDifficulty.Intermediate,
            "Order'lar üzerinde dönerken order.Customer.Name'e dokunmak her order için ek bir sorgu tetikliyor. Standart çözüm nedir?",
            "Bu N+1 problemidir — ilişkiyi Include ile önden yükle (veya yalnızca gereken sütunları projekte et) ki tek bir sorguya dönüşsün.",
            90, ["performance", "n+1"], ("AsNoTracking ekle", false), ("Customer'ı önden yüklemek için Include kullan", true), ("Döngüyü bir transaction'a sar", false), ("Bağlantı havuzu boyutunu artır", false));
        yield return Mc(ef, TopicDifficulty.Intermediate,
            "Tek bir SaveChanges çağrısı hangi transaction garantisini sağlar?",
            "O SaveChanges'teki tüm değişiklikler tek bir örtük transaction içinde işlenir — hep birlikte başarılı olur ya da geri alınır.",
            60, ["transactions"], ("Hiçbiri — her ifade bağımsız işlenir", false), ("İzlenen tüm değişiklikler tek bir transaction'da atomik olarak işlenir", true), ("Yalnızca insert'ler transaction içindedir", false));
        yield return Mc(ef, TopicDifficulty.Advanced,
            "Bir EF Core value converter'a ne zaman başvurursun?",
            "Value converter'lar bir CLR şekliyle bir sütun şekli arasında çeviri yapar — örn. bir listeyi JSON metni olarak veya bir enum'ı string olarak saklamak.",
            75, ["mapping"], ("Bir tabloyu yeniden adlandırmak için", false), ("Bir CLR türünü, JSON'a serileştirilmiş bir liste gibi farklı bir veritabanı temsiline eşlemek için", true), ("Change tracking'i hızlandırmak için", false));
        yield return Sa(ef, TopicDifficulty.Fundamental,
            "Bir sorguda ilişkili bir navigation özelliğini önden (eager) yükleyen yöntem hangisidir?",
            "Include (daha derin seviyeler için ThenInclude ile) ilişkili veriyi sorguya join'ler.",
            45, ["querying"], "include");
        yield return Sa(ef, TopicDifficulty.Advanced,
            "EF Core'a, tek bir join yerine dahil edilen her koleksiyon için ayrı sorgu çalıştırmasını hangi operatör söyler?",
            "AsSplitQuery, birden çok koleksiyon dahil edilirken kartezyen patlamayı önler.",
            60, ["performance"], "assplitquery", "split query", "bölünmüş sorgu");
        yield return Sa(ef, TopicDifficulty.Intermediate,
            "Varlık durumlarını (Added, Modified, Deleted) kaydeden DbContext bileşeninin adı nedir?",
            "Change tracker (değişiklik izleyici) — SaveChanges, SQL üretmek için onu okur.",
            45, ["ef-core"], "change tracker", "değişiklik izleyici", "changetracker");
        yield return Sc(ef, TopicDifficulty.Advanced,
            "Bir panel sorgusu veri büyüdükçe yavaşladı. Teşhis adımlarını ve EF Core ile veritabanında değerlendireceğin kaldıraçları anlat.",
            "Üretilen SQL'i yakala, onu EXPLAIN et, indeksleri kontrol et, yalnızca gereken sütunları projekte et, okumalar için tracking'i kapat ve split query'leri ya da toplamayı (aggregation) veritabanına taşımayı değerlendir.",
            240, ["indeks", "tracking", "projeksiyon", "sql"]);

        // ── Temiz Mimari ──────────────────────────────────────────────────
        yield return Sc(clean, TopicDifficulty.Advanced,
            "Bir controller doğrudan DbContext ve eşleme (mapping) mantığı kullanıyor. Hangi mimari kaygılar ihlal ediliyor?",
            "Taşıma (transport), uygulama ve kalıcılık kaygıları tek bir katmana sızıyor.",
            180, ["sınır", "ayrım", "bağımlılık", "uygulama"]);
        yield return Mc(clean, TopicDifficulty.Fundamental,
            "Temiz Mimari'de kaynak kod bağımlılıkları hangi yöne işaret etmelidir?",
            "Bağımlılıklar içe doğru işaret eder: dış katmanlar (UI, altyapı) iç katmanlara (uygulama, alan) bağımlıdır, asla tersi değil.",
            60, ["dependencies"], ("Dışa doğru, altyapıya", false), ("İçe doğru, alana (domain)", true), ("Arayüz kullanılırsa her iki yön de sorun değil", false));
        yield return Mc(clean, TopicDifficulty.Fundamental,
            "Kullanım senaryoları (uygulamaya özgü iş kuralları) nerede yaşar?",
            "Uygulama katmanı kullanım senaryolarını orkestre eder; alan (domain) kurumsal kuralları tutar, dış katmanlar yalnızca uyarlar.",
            60, ["layers"], ("Alan (domain) katmanı", false), ("Uygulama katmanı", true), ("Altyapı katmanı", false), ("API katmanı", false));
        yield return Mc(clean, TopicDifficulty.Intermediate,
            "Uygulama katmanının kalıcılığa ihtiyacı var. Repository arayüzü ve onun EF Core uygulaması nereye ait?",
            "Arayüz, onu tüketen iç katmana aittir; uygulama ise altyapıda yaşar — bu tersine çevirme çekirdeği kalıcılıktan bağımsız tutar.",
            90, ["dependencies"], ("İkisi de altyapıda", false), ("Arayüz uygulama/alan katmanında, uygulama altyapıda", true), ("Arayüz API katmanında, uygulama alanda", false));
        yield return Mc(clean, TopicDifficulty.Intermediate,
            "Bir mimari sınırda DTO'nun temel görevi nedir?",
            "DTO'lar, kablo/kalıcılık şeklini alan nesnelerinden ayırır; böylece iç modeller sözleşmeleri bozmadan gelişebilir.",
            60, ["boundaries"], ("İş değişmezlerini (invariant) zorunlu kılar", false), ("Alanın iç yapısını açığa çıkarmadan veriyi bir sınırın ötesine taşır", true), ("Sorgu sonuçlarını önbelleğe alır", false));
        yield return Sa(clean, TopicDifficulty.Fundamental,
            "Hangi SOLID ilkesi, üst seviye modüllerin somut uygulamalar yerine soyutlamalara bağımlı olması gerektiğini söyler?",
            "Bağımlılığın Tersine Çevrilmesi İlkesi (Dependency Inversion) — SOLID'deki D ve Temiz Mimari'nin içe dönük bağımlılıklarının arkasındaki mekanizma.",
            45, ["solid"], "bağımlılığın tersine çevrilmesi", "dependency inversion", "dependency inversion principle", "dip");
        yield return Sa(clean, TopicDifficulty.Fundamental,
            "EF Core varlık yapılandırmaları ve migration'lar hangi katmana aittir?",
            "Altyapı — kalıcılık eşlemesi, uygulamaya ait soyutlamaların arkasına gizlenmiş bir uygulama ayrıntısıdır.",
            45, ["layers"], "altyapı", "infrastructure");
        yield return Sa(clean, TopicDifficulty.Intermediate,
            "Kimliği tamamen değerleriyle tanımlanan, küçük ve değişmez bir alan nesnesine (örn. Money, DateRange) ne ad verilir?",
            "Değer nesnesi (value object) — değerleri eşit olduğunda eşittir, kendine ait bir kimliği yoktur.",
            60, ["domain"], "değer nesnesi", "value object");

        // ── CQRS ──────────────────────────────────────────────────────────
        yield return Sa(cqrs, TopicDifficulty.Advanced,
            "CQRS'te okuma modelleri neden komut işleyicilerden (command handler) ayrılmalıdır?",
            "Okuma yolları ile yazma yollarının farklı optimizasyon ve tutarlılık kaygıları vardır.",
            120, ["cqrs"], "okuma optimizasyonu", "yazma modeli", "ayrı", "read optimization");
        yield return Mc(cqrs, TopicDifficulty.Fundamental,
            "Bir komut (command) ile bir sorgu (query) arasındaki belirleyici fark nedir?",
            "Komutlar durumu değiştirir ve az şey döndürür ya da hiçbir şey döndürmez; sorgular veri döndürür ve durumu değiştirmemelidir.",
            45, ["cqrs"], ("Komutlar sorgulardan daha hızlıdır", false), ("Komutlar durumu değiştirir; sorgular yalnızca okur", true), ("Sorgular farklı bir iş parçacığında çalışır", false));
        yield return Mc(cqrs, TopicDifficulty.Intermediate,
            "CQRS hattında girdi doğrulaması nereye aittir?",
            "Handler'dan önce çalışan bir hat adımına (validator/behavior) — böylece handler'lar yalnızca geçerli komutlar görür.",
            75, ["validation"], ("Her controller action'ının içine", false), ("Handler çalışmadan önce bir pipeline behavior'a", true), ("Yalnızca veritabanında kısıtlamalarla", false));
        yield return Mc(cqrs, TopicDifficulty.Advanced,
            "Olaylarla beslenen ayrı, denormalize bir okuma deposu ekliyorsun. Arayüz artık hangi tutarlılık özelliğini tolere etmelidir?",
            "Nihai tutarlılık (eventual consistency) — okuma deposu yazma deposunun kısa süre gerisinde kalır, bu yüzden okumalar en son yazmayı yansıtmayabilir.",
            90, ["consistency"], ("Güçlü tutarlılık", false), ("Nihai tutarlılık (eventual consistency)", true), ("Serializable izolasyon", false));
        yield return Sa(cqrs, TopicDifficulty.Fundamental,
            "Bir komutu alıp onu tek işleyicisine yönlendiren bileşene ne ad verilir?",
            "Bir dispatcher (veya mediator), bir komut için handler'ı çözümler ve onu çağırır.",
            45, ["cqrs"], "dispatcher", "mediator", "command dispatcher");
        yield return Sa(cqrs, TopicDifficulty.Intermediate,
            "Loglama ve doğrulama gibi kesişen kaygılar (cross-cutting), MediatR tarzı hatlarda her handler'ı hangi desenle sarar?",
            "Pipeline behavior'lar — handler çağrısının etrafında birleşen dekoratörler.",
            60, ["pipeline"], "pipeline behavior", "behavior", "dekoratör", "decorator");
        yield return Sa(cqrs, TopicDifficulty.Fundamental,
            "CQRS kısaltması neyin açılımıdır?",
            "Command Query Responsibility Segregation (Komut Sorgu Sorumluluğu Ayrımı).",
            30, ["cqrs"], "command query responsibility segregation", "komut sorgu sorumluluğu ayrımı");
        yield return Sc(cqrs, TopicDifficulty.Advanced,
            "Bir takım arkadaşın, saflık uğruna her okumanın tam alan modelinden geçmesi gerektiğinde ısrar ediyor. Yüksek trafikli bir liste endpoint'i için CQRS duruşunu savun.",
            "Okumalar alanı atlayabilir: performans için doğrudan bir okuma modelinden DTO'lara projekte et; değişmezler (invariant) yalnızca yazma tarafında önemlidir.",
            180, ["okuma modeli", "projeksiyon", "dto", "performans"]);

        // ── JWT Kimlik Doğrulama ──────────────────────────────────────────
        yield return Sa(jwt, TopicDifficulty.Intermediate,
            "ASP.NET Core yetkilendirmesinde kararlı kullanıcı tanımlayıcısı olarak yaygın biçimde hangi claim kullanılır?",
            "NameIdentifier, mevcut principal için alışılmış kararlı tanımlayıcı claim'idir.",
            60, ["jwt"], "nameidentifier", "sub");
        yield return Mc(jwt, TopicDifficulty.Fundamental,
            "Bir JWT'nin üç parçası, sırasıyla nelerdir?",
            "header.payload.signature — base64url ile kodlanmış iki JSON bölümü artı bunların üzerindeki bir imza.",
            45, ["jwt"], ("Header, payload, signature", true), ("Issuer, audience, secret", false), ("Claim, scope, role", false));
        yield return Mc(jwt, TopicDifficulty.Intermediate,
            "Bir tarayıcı SPA'sının bir refresh token'ı tutması için en güvenli alışılmış yer neresidir?",
            "HttpOnly, Secure bir çerez — JavaScript onu okuyamaz, bu da XSS ile token hırsızlığını köreltir; localStorage ise enjekte edilen herhangi bir betikçe okunabilir.",
            90, ["security"], ("localStorage", false), ("HttpOnly Secure bir çerez", true), ("Global bir JavaScript değişkeni", false), ("URL fragment'ı", false));
        yield return Mc(jwt, TopicDifficulty.Intermediate,
            "Bir JWT imzası gerçekte neyi garanti eder?",
            "Bütünlük ve gerçeklik — payload değiştirilmemiştir ve bir anahtar sahibince üretilmiştir. Payload'ı ŞİFRELEMEZ; herkes onu okuyabilir.",
            75, ["security"], ("Payload şifrelenmiştir", false), ("Token kurcalanmamıştır ve anahtar sahibinden gelir", true), ("Token yeniden oynatılamaz (replay)", false));
        yield return Sa(jwt, TopicDifficulty.Fundamental,
            "Kayıtlı hangi JWT claim'i sona erme zamanını taşır?",
            "exp — sonrasında doğrulamanın başarısız olması gereken bir Unix zaman damgası.",
            30, ["jwt"], "exp");
        yield return Sa(jwt, TopicDifficulty.Intermediate,
            "HS256, token'ları ne tür bir kriptografik şemayla imzalar?",
            "Simetrik bir HMAC — RS256'nın açık/özel anahtar çiftinin aksine, aynı paylaşılan sır hem imzalar hem doğrular.",
            60, ["crypto"], "hmac", "simetrik", "symmetric");
        yield return Sa(jwt, TopicDifficulty.Fundamental,
            "Bir istemci, bearer token'ı alışıldık biçimde hangi HTTP başlığında gönderir?",
            "Authorization: Bearer <token>.",
            30, ["http"], "authorization", "authorization: bearer");
        yield return Sc(jwt, TopicDifficulty.Advanced,
            "API'nin erişim token'ları bir loglama hatasıyla sızdı. Acil kontrol altına alma adımlarını ve gelecekteki etki alanını sınırlayan tasarım değişikliklerini anlat.",
            "Kısa sona erme süresi maruziyeti sınırlar; imzalama anahtarlarını döndür (rotate) ve refresh token'ları hemen iptal et; yeniden kullanım tespitli refresh token rotasyonu ekle, token'ları loglardan uzak tut ve her yerde https'i zorunlu kıl.",
            240, ["rotasyon", "iptal", "sona erme", "https"]);

        // ── Önbellekleme Stratejisi ───────────────────────────────────────
        yield return Sc(caching, TopicDifficulty.Advanced,
            "Pahalı bir panel sorgusu her istekte çalıştırılıyor. Önbellekleme kararını hangi ödünleşimler yönlendirmeli?",
            "Cevap; önbellek tazeliğinden, geçersiz kılmadan, okuma yükünden ve hata durumlarından söz etmeli.",
            180, ["tazelik", "geçersiz kılma", "gecikme", "tutarlılık"]);
        yield return Mc(caching, TopicDifficulty.Intermediate,
            "Hangi sıralama cache-aside desenini tarif eder?",
            "Uygulama önce önbelleği kontrol eder, ıskalamada (miss) kaynaktan yükler, sonra sonucu tekrar önbelleğe yazar.",
            75, ["patterns"], ("Her yazmada eşzamanlı olarak hem önbelleğe hem veritabanına yaz", false), ("Önbelleği kontrol et; ıskalamada kaynaktan yükle ve önbelleği doldur", true), ("Önbellek kendisi tembelce veritabanını sorgular", false));
        yield return Mc(caching, TopicDifficulty.Advanced,
            "Sıcak bir önbellek anahtarı sona eriyor ve 500 eşzamanlı istek onu yeniden oluşturmak için veritabanına yükleniyor. Bu izdihamı (stampede) hangi teknik önler?",
            "Yeniden oluşturmayı kilitle/tek-uçuş (single-flight) yap; böylece bir çağıran yeniden hesaplarken diğerleri bekler veya bayat veri sunar — dağıtılmış (jitter'lı) TTL'ler de sona ermeyi yaymaya yardımcı olur.",
            90, ["stampede"], ("Her anahtarda daha kısa TTL'ler", false), ("Yalnızca bir isteğin değeri yeniden hesaplaması için tek-uçuş kilitleme", true), ("Değeri iki önbellekte saklama", false));
        yield return Mc(caching, TopicDifficulty.Fundamental,
            "Aşağıdakilerden hangisi agresif önbelleklemeye en uygun adaydır?",
            "Yavaş değişen referans verisi (örn. ülke listesi) uzun TTL'leri tolere eder; kullanıcı bazlı bakiyeler ve stok sayıları tehlikeli biçimde hızlı bayatlar.",
            60, ["strategy"], ("Bir kullanıcının hesap bakiyesi", false), ("Bir ülke/para birimi referans listesi", true), ("İndirim sırasında canlı stok sayıları", false));
        yield return Sa(caching, TopicDifficulty.Fundamental,
            "Hangi tahliye (eviction) politikası en uzun süredir kullanılmayan girdiyi kaldırır?",
            "LRU — en az son kullanılan (least recently used).",
            30, ["eviction"], "lru", "en az son kullanılan", "least recently used");
        yield return Sa(caching, TopicDifficulty.Intermediate,
            "Hangi HTTP yanıt başlığı, istemcilerin ve proxy'lerin bir yanıtı nasıl önbelleğe alabileceğini denetler?",
            "Cache-Control (max-age, no-store, public/private…).",
            45, ["http"], "cache-control");
        yield return Sa(caching, TopicDifficulty.Fundamental,
            ".NET ile dağıtık önbellek olarak en yaygın kullanılan bellek içi veri deposunun adını yaz.",
            "Redis — IDistributedCache veya StackExchange.Redis üzerinden.",
            30, ["redis"], "redis");
        yield return Mc(caching, TopicDifficulty.Advanced,
            "Write-through (yazarken geçirmeli) önbelleklemede bir yazmada ne olur?",
            "Yazma, önbelleğe ve alttaki depoya birlikte gider; yazma gecikmesi pahasına ikisini tutarlı tutar.",
            75, ["patterns"], ("Yalnızca önbellek güncellenir; depo sonra senkronize olur", false), ("Önbellek ve arka depo senkron olarak birlikte güncellenir", true), ("Önbellek girdisi silinir ve tembelce yeniden yüklenir", false));

        // ── PostgreSQL ────────────────────────────────────────────────────
        yield return Sa(pg, TopicDifficulty.Intermediate,
            "Eşitlik aramaları için genellikle varsayılan seçim olan PostgreSQL indeks türü hangisidir?",
            "B-tree indeksleri, eşitlik ve aralık aramaları için genel varsayılandır.",
            45, ["postgres"], "btree", "b-tree", "b-ağacı");
        yield return Mc(pg, TopicDifficulty.Intermediate,
            "EXPLAIN ANALYZE, sade EXPLAIN'in yapmadığı neyi yapar?",
            "Sorguyu gerçekten çalıştırır ve planla birlikte gerçek süreleri ve satır sayılarını raporlar — çok değerlidir, ancak yazma işlemlerinde çalıştırmaya dikkat et.",
            75, ["performance"], ("Planı JSON olarak biçimlendirir", false), ("Sorguyu çalıştırır ve gerçek süreleri ile satır sayılarını gösterir", true), ("Planlayıcı için tablo istatistiklerini analiz eder", false));
        yield return Mc(pg, TopicDifficulty.Intermediate,
            "PostgreSQL varsayılan olarak hangi transaction izolasyon seviyesini kullanır?",
            "Read Committed — her ifade, o ifade başlamadan önce işlenmiş (commit) veriyi görür.",
            60, ["transactions"], ("Serializable", false), ("Read Committed", true), ("Repeatable Read", false), ("Read Uncommitted", false));
        yield return Mc(pg, TopicDifficulty.Advanced,
            "Kısmi (partial) indeks ne zaman doğru araçtır?",
            "Sorgular yalnızca öngörülebilir bir satır dilimine dokunduğunda (örn. WHERE status = 'active') — indeks küçük kalır ve diğer satırlara yazmalar onu atlar.",
            90, ["indexes"], ("Tablo küçük olduğunda", false), ("Sorgular, satırların bir alt kümesiyle eşleşen sabit bir koşula göre filtrelediğinde", true), ("Tüm satırlarda benzersizliğe ihtiyaç duyduğunda", false));
        yield return Sa(pg, TopicDifficulty.Fundamental,
            "PostgreSQL planlayıcısının bir sorgu için seçtiği yürütme planını hangi SQL komutu gösterir?",
            "EXPLAIN (gerçek sayılar için isteğe bağlı olarak ANALYZE ile çalıştırılır).",
            30, ["performance"], "explain");
        yield return Sa(pg, TopicDifficulty.Advanced,
            "jsonb kapsama sorgularına (@> operatörü) verimli hizmet eden indeks türü hangisidir?",
            "GIN — genelleştirilmiş ters indeksler (generalized inverted index), jsonb ve diziler için çok değerli kapsamayı ele alır.",
            60, ["indexes", "jsonb"], "gin");
        yield return Sa(pg, TopicDifficulty.Intermediate,
            "PostgreSQL'in eşzamanlılık modelinde MVCC neyin kısaltmasıdır?",
            "Multiversion Concurrency Control (Çok Sürümlü Eşzamanlılık Denetimi) — okuyucular yazanları bloklamak yerine anlık görüntüler (snapshot) görür.",
            45, ["concurrency"], "multiversion concurrency control", "çok sürümlü eşzamanlılık denetimi", "multi-version concurrency control");
        yield return Sc(pg, TopicDifficulty.Advanced,
            "Yoğun güncellenen bir tablo diskte büyümeye devam ediyor ve satır sayısı sabit olsa bile taramalar yavaşlıyor. Ne olduğunu ve PostgreSQL'in bununla nasıl başa çıktığını açıkla.",
            "Güncellemeler MVCC altında ölü satırlar (dead tuple) bırakır — bu tablo şişmesidir (bloat); VACUUM (ve düzgün ayarlanmış bir autovacuum) ölü alanı geri kazanır, fillfactor/HOT güncellemeleri ise çalkantıyı azaltır.",
            240, ["vacuum", "ölü", "autovacuum", "şişme"]);
    }
}
