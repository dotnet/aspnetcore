// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.Internal;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.DataProtection.XmlEncryption;
using Microsoft.Extensions.Options;

var dayCount = 365;
var instanceCount = 10;
var seed = 0;

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "-d":
        case "--days":
            if (i + 1 < args.Length && int.TryParse(args[i + 1], out var d) && d > 0)
            {
                dayCount = d;
                i++;
            }
            else
            {
                Console.WriteLine("Invalid argument: days must be a positive integer.");
                PrintUsage();
                return;
            }
            break;

        case "-i":
        case "--instances":
            if (i + 1 < args.Length && int.TryParse(args[i + 1], out var inst) && inst > 0)
            {
                instanceCount = inst;
                i++;
            }
            else
            {
                Console.WriteLine("Invalid argument: instances must be a positive integer.");
                PrintUsage();
                return;
            }
            break;

        case "-s":
        case "--seed":
            if (i + 1 < args.Length && int.TryParse(args[i + 1], out var s) && s > 0)
            {
                seed = s;
                i++;
            }
            else
            {
                // 0 is reserved to mean "use a random seed"
                Console.WriteLine("Invalid argument: seed must be a positive integer.");
                PrintUsage();
                return;
            }
            break;

        case "-h":
        case "--help":
            PrintUsage();
            return;

        default:
            Console.WriteLine($"Unknown argument: {args[i]}");
            PrintUsage();
            return;
    }
}

// Don't pick 0 because we want to end up with something the user can specify
while (seed == 0)
{
    seed = Random.Shared.Next();
}

var startTime = DateTimeOffset.ParseExact("2015-03-01 00:00:00Z", "u", CultureInfo.InvariantCulture).UtcDateTime;
var endTime = startTime.AddDays(dayCount);

// Splitting this into two randoms makes it easier to compare across product changes that affect
// how often mock components are accessed (e.g. by reducing the number of network calls)
var productRandom = new Random(seed);
var simulatorRandom = new Random(productRandom.Next());

// These are shared across instances to simulate a shared backend
var encryptor = new FlakyXmlEncryptor(simulatorRandom, pFail: 0); // Causes keyring update to throw - not interesting
var decryptor = new FlakyXmlDecryptor(simulatorRandom, pFail: 0.03);
var repository = new FlakyXmlRepository(simulatorRandom, pFail: 0); // Causes keyring update to throw - not interesting

// TODO: it would be nice to simulate instances appearing and disappearing over the course of the run
var appInstances = new AppInstance[instanceCount];
var descriptors = new KeyRingDescriptor[instanceCount]; // parallel array
var defaultKeyIds = new Guid[instanceCount]; // This is redundant with descriptors, but we need it in this form

// Stagger the start times of the instances to avoid using key IDs as tie-breakers
var startOffsetsMs = Enumerable.Range(0, instanceCount).ToArray();
// To properly simulate the startup race in a deterministic way, we'd have to manipulate the repository
// from here, telling it when each pair of GetAllKeys requests begins so that it can randomly decide
// whether or not to return an empty list the first time.  The startup race is fairly well understood,
// so we won't bother, focusing instead on steady state sync issues.

// Initialize all instances
for (var i = 0; i < instanceCount; i++)
{
    var appInstance = new AppInstance(i, encryptor, decryptor, repository, productRandom);
    var descriptor = appInstance.RefreshKeyRingAndGetDescriptor(startTime.AddMilliseconds(startOffsetsMs[i]));

    appInstances[i] = appInstance;
    descriptors[i] = descriptor;
    defaultKeyIds[i] = descriptor.DefaultKeyId;
}

// Initialize all metrics
for (var i = 0; i < instanceCount; i++)
{
    appInstances[i].UpdateMetrics(startTime.AddMilliseconds(startOffsetsMs[i]), defaultKeyIds);
}

// Step through the next N refresh times until dayCount days have elapsed
while (true)
{
    var instanceIndex = GetNextAppInstanceIndex(); // Find the instance with the next closest refresh time
    var appInstance = appInstances[instanceIndex];
    var oldDescriptor = descriptors[instanceIndex];
    var refreshTime = oldDescriptor.ExpirationTimeUtc;

    if (refreshTime >= endTime)
    {
        break;
    }

    var newDescriptor = appInstance.RefreshKeyRingAndGetDescriptor(refreshTime);
    var newDefaultKeyId = newDescriptor.DefaultKeyId;

    descriptors[instanceIndex] = newDescriptor;

    if (oldDescriptor.DefaultKeyId == newDescriptor.DefaultKeyId)
    {
        // None of the metrics can have changed if all instances have the same default keys as they did last step
        continue;
    }

    var otherInstanceHasSameDefaultKey = defaultKeyIds.Contains(newDefaultKeyId);
    defaultKeyIds[instanceIndex] = newDefaultKeyId; // Set this *after* the contains check

    if (!otherInstanceHasSameDefaultKey)
    {
        // If this was not previously a default key, then we need to update the metrics of *other* instances
        // (in case they don't know about it)
        for (var i = 0; i < instanceCount; i++)
        {
            if (i == instanceIndex)
            {
                continue;
            }

            // We can't have changed the set of known keys, so we only need to update a single metric
            if (appInstances[i].UpdateSingleKeyMetric(refreshTime, newDescriptor.DefaultKeyId))
            {
                Console.WriteLine($"[{i}] missing default key of [{instanceIndex}] at {refreshTime}");
            }
        }
    }

    // Regardless of whether or not the default key for this instance is new, we have to update the metrics of this instance
    // (in case it has learned of keys it was previously missing)
    appInstance.UpdateMetrics(refreshTime, defaultKeyIds);
}

// Finalize all metrics
foreach (var appInstance in appInstances)
{
    appInstance.FinalizeMetrics(endTime);
}

// See CumulativeProblemTime for more details on what this score means
Console.WriteLine("Individual missing key scores (lower is better)");
var totalProblemTime = TimeSpan.Zero;
for (var i = 0; i < instanceCount; i++)
{
    var appInstance = appInstances[i];
    var cumulativeProblemTime = appInstance.CumulativeProblemTime;
    Console.WriteLine($"Instance {i}: {Math.Round(cumulativeProblemTime.TotalHours, 2)} hours");

    totalProblemTime += cumulativeProblemTime;
}
Console.WriteLine($"Total missing key score (lower is better) = {Math.Round(totalProblemTime.TotalHours, 2)} hours");
Console.WriteLine($"Elapsed Time = {endTime - startTime}");
Console.WriteLine($"IXmlRepository.GetAllElements Call Count = {repository.GetAllElementsCallCount}");
Console.WriteLine($"IXmlRepository.StoreElement Call Count = {repository.StoreElementCallCount}");
Console.WriteLine($"IXmlEncryptor.Encrypt Call Count = {encryptor.EncryptCallCount}");
Console.WriteLine($"IXmlDecryptor.Decrypt Call Count = {decryptor.DecryptCallCount}");
Console.WriteLine($"Seed = {seed}");

int GetNextAppInstanceIndex()
{
    // For the number of instances we have, it's not worth implementing a heap -
    // just linear search
    var minIndex = 0;
    var min = descriptors[minIndex].ExpirationTimeUtc;
    for (var i = 1; i < instanceCount; i++)
    {
        var refreshTime = descriptors[i].ExpirationTimeUtc;
        if (refreshTime < min)
        {
            min = refreshTime;
            minIndex = i;
        }
    }
    return minIndex;
}

static void PrintUsage()
{
    Console.WriteLine("Usage: your_program [-d days] [-i instances] [-s seed]");
}

/// <summary>
/// A mock authenticated encryptor descriptor that always returns the same secret.
/// </summary>
sealed class MockAuthenticatedEncryptorDescriptor : IAuthenticatedEncryptorDescriptor
{
    private static readonly XElement serializedDescriptor = XElement.Parse(@"
        <theElement>
            <secret enc:requiresEncryption='true' xmlns:enc='http://schemas.asp.net/2015/03/dataProtection'>
            <![CDATA[This is a secret value.]]>
            </secret>
        </theElement>");

    XmlSerializedDescriptorInfo IAuthenticatedEncryptorDescriptor.ExportToXml() =>
        new(serializedDescriptor, typeof(IAuthenticatedEncryptorDescriptorDeserializer)); // This shouldn't be an interface, but we control the activator
}

/// <summary>
/// A mock algorithm configuration that always returns the same descriptor.
/// </summary>
sealed class MockAlgorithmConfiguration(IAuthenticatedEncryptorDescriptor descriptor) : AlgorithmConfiguration
{
    public override IAuthenticatedEncryptorDescriptor CreateNewDescriptor() => descriptor;
}

/// <summary>
/// A mock authenticated encryptor descriptor deserializer that always returns the same descriptor.
/// </summary>
/// <param name="descriptor"></param>
sealed class MockAuthenticatedEncryptorDescriptorDeserializer(IAuthenticatedEncryptorDescriptor descriptor) : IAuthenticatedEncryptorDescriptorDeserializer
{
    IAuthenticatedEncryptorDescriptor IAuthenticatedEncryptorDescriptorDeserializer.ImportFromXml(XElement element) => descriptor;
}

/// <summary>
/// A mock activator we use to hijack deserialization.  We know that our serializers specify
/// interface types, which would usually be prohibited, so we decode exactly those types to
/// instances known in advance.
/// </summary>
sealed class MockActivator(IXmlDecryptor decryptor, IAuthenticatedEncryptorDescriptorDeserializer descriptorDeserializer) : IActivator
{
    object IActivator.CreateInstance(Type type, string _friendlyName) => type switch
    {
        Type t when t == typeof(IXmlDecryptor) => decryptor,
        Type t when t == typeof(IAuthenticatedEncryptorDescriptorDeserializer) => descriptorDeserializer,
        _ => throw new InvalidOperationException(),
    };
}

/// <summary>
/// A mock authenticated encryptor that only applies the identity function (i.e. does nothing).
/// </summary>
sealed class MockAuthenticatedEncryptor : IAuthenticatedEncryptor
{
    byte[] IAuthenticatedEncryptor.Decrypt(ArraySegment<byte> ciphertext, ArraySegment<byte> _additionalAuthenticatedData) => ciphertext.ToArray();
    byte[] IAuthenticatedEncryptor.Encrypt(ArraySegment<byte> plaintext, ArraySegment<byte> _additionalAuthenticatedData) => plaintext.ToArray();
}

/// <summary>
/// An almost trivial mock authenticated encryptor factory that always returns the same encryptor,
/// but not before ensuring that the key descriptor can be retrieved.  This causes realistic failures
/// during default key resolution.
/// </summary>
/// <param name="authenticatedEncryptor"></param>
sealed class MockAuthenticatedEncryptorFactory(IAuthenticatedEncryptor authenticatedEncryptor) : IAuthenticatedEncryptorFactory
{
    IAuthenticatedEncryptor IAuthenticatedEncryptorFactory.CreateEncryptorInstance(IKey key)
    {
        _ = key.Descriptor; // Trigger decryption, which may fail
        return authenticatedEncryptor; // Succeed if retrieving the descriptor didn't throw
    }
}

/// <summary>
/// A representation of a single instance of an application that uses Data Protection.
/// Ordinarily, each instance would run in its own container or, at least, process, but
/// that would slow down our simulation without making interesting failures more likely.
///
/// Exposes enough information to trigger key ring refreshes at appropriate (simulated)
/// times and tracks the missing-key-time score for this instance.
/// </summary>
[DebuggerDisplay("AppInstance {_instanceNumber}")]
sealed class AppInstance
{
    private readonly int _instanceNumber;
    private readonly ICacheableKeyRingProvider _cacheableKeyRingProvider;

    private readonly Dictionary<Guid, DateTimeOffset> _missingSinceMap = new();
    private TimeSpan _cumulativeProblemTime = TimeSpan.Zero;

    private DateTimeOffset _now;
    private HashSet<Guid> _knownKeyIds; // As of _now

    /// <param name="instanceNumber">Purely for logging.</param>
    /// <param name="encryptor">Represents shared access to a backend like AKV.</param>
    /// <param name="decryptor">Represents shared access to a backend like AKV.</param>
    /// <param name="repository">Represents access to a shared backend like Azure Blob Storage.</param>
    /// <param name="productRandom">Passed to product code to force determinism.</param>
    public AppInstance(int instanceNumber, IXmlEncryptor encryptor, IXmlDecryptor decryptor, IXmlRepository repository, Random productRandom)
    {
        _instanceNumber = instanceNumber;

        // The only descriptor we'll use
        var descriptor = new MockAuthenticatedEncryptorDescriptor();

        // The factory always returns the only descriptor
        var authenticatedEncryptorConfiguration = new MockAlgorithmConfiguration(descriptor);

        // Any xml element deserializes to the only descriptor
        var descriptorDeserializer = new MockAuthenticatedEncryptorDescriptorDeserializer(descriptor);

        // We control deserialization by hooking activation
        var activator = new MockActivator(decryptor, descriptorDeserializer);

        // We don't actually want to burn CPU by doing real encryption
        var authenticatedEncryptor = new MockAuthenticatedEncryptor();

        var authenticatedEncryptorFactory = new MockAuthenticatedEncryptorFactory(authenticatedEncryptor);

        var keyManagementOptions = Options.Create(new KeyManagementOptions()
        {
            AuthenticatedEncryptorConfiguration = authenticatedEncryptorConfiguration,
            XmlRepository = repository,
            XmlEncryptor = encryptor,
        });
        keyManagementOptions.Value.AuthenticatedEncryptorFactories.Add(authenticatedEncryptorFactory);

        var keyManager = new XmlKeyManager(keyManagementOptions, activator)
        {
            GetUtcNow = () => _now,
        };
        var defaultKeyResolver = new DefaultKeyResolver(keyManagementOptions);

        _cacheableKeyRingProvider = new KeyRingProvider(keyManager, keyManagementOptions, defaultKeyResolver)
        {
            JitterRandom = productRandom,
        };
    }

    /// <summary>
    /// The problem-time-score for this instance.
    ///
    /// This score is a little hard to explain.  Suppose there were two instances, A and B.
    /// If for a period of length T, B had a default key that was unknown to A, then T would
    /// be added to A's score.  So, if A didn't know about B's default key for 5 minutes at
    /// the start of the run and another 3 hours later on (probably a different default key
    /// by then), then A's score would be 3:05.  If there were a third instance, C, and
    /// A didn't know about C's default key for 2:03, then A's total score would be 5:08,
    /// regardless of whether or not B and C had any overlap in their missing key ranges.
    /// This reflects the fact that not knowing the key of either B or C is *worse* than
    /// just not knowing the key of one of them because it increases the likelihood of a
    /// session migrating to A and hitting a missing key error.
    /// </summary>
    public TimeSpan CumulativeProblemTime => _cumulativeProblemTime;

    /// <summary>
    /// Trigger a key ring refresh and return enough information to update metrics
    /// and advance the simulation.
    /// </summary>
    public KeyRingDescriptor RefreshKeyRingAndGetDescriptor(DateTimeOffset now)
    {
        // Update this before calling GetCacheableKeyRing so that it's available to XmlKeyManager
        _now = now;

        var keyRing = _cacheableKeyRingProvider.GetCacheableKeyRing(now);

        _knownKeyIds = new(((KeyRing)keyRing.KeyRing).GetAllKeyIds());

        return new KeyRingDescriptor(keyRing.KeyRing.DefaultKeyId, keyRing.ExpirationTimeUtc);
    }

    /// <param name="defaultKeyIds">The default keys of all key rings, including this one.</param>
    public void UpdateMetrics(DateTimeOffset now, IReadOnlyCollection<Guid> defaultKeyIds)
    {
        Debug.Assert(now >= _now);
        var knownKeyIds = _knownKeyIds;

        foreach (var keyId in knownKeyIds)
        {
            if (_missingSinceMap.TryGetValue(keyId, out var missingSince))
            {
                _missingSinceMap.Remove(keyId);
                _cumulativeProblemTime += now - missingSince;
            }
        }

        var i = 0;
        foreach (var defaultKeyId in defaultKeyIds)
        {
            if (UpdateSingleKeyMetric(now, defaultKeyId))
            {
                Console.WriteLine($"[{_instanceNumber}] missing default key of [{i}] at {now}");
            }
            i++;
        }
    }

    /// <summary>
    /// Ensure that a single key is marked as missing, if appropriate.
    /// </summary>
    /// <returns>True if the key needed to be marked as missing (i.e. it is and was not already known to be).</returns>
    public bool UpdateSingleKeyMetric(DateTimeOffset now, Guid defaultKeyId)
    {
        if (!_knownKeyIds.Contains(defaultKeyId) && !_missingSinceMap.ContainsKey(defaultKeyId))
        {
            _missingSinceMap[defaultKeyId] = now;
            return true;
        }

        return false;
    }

    /// <summary>
    /// The last step of the simulation just truncates all existing missing key ranges -
    /// there's no corresponding key ring refresh.  All keys missing prior to this point
    /// are considered to be known now, for scoring purposes.
    /// </summary>
    public void FinalizeMetrics(DateTimeOffset now)
    {
        foreach (var missingSince in _missingSinceMap.Values)
        {
            _cumulativeProblemTime += now - missingSince;
        }

        _missingSinceMap.Clear(); // Make it idempotent
    }
}

/// <summary>
/// <see cref="AppInstance"/> state that needs to be exposed to the simulator loop.
/// </summary>
readonly struct KeyRingDescriptor(Guid defaultKeyId, DateTimeOffset expirationTimeUtc)
{
    public readonly Guid DefaultKeyId => defaultKeyId;
    public readonly DateTimeOffset ExpirationTimeUtc => expirationTimeUtc;
}

/// <summary>
/// A helper class exposing a <see cref="MaybeFail"/> method that throws with some probability.
/// On each invocation, a decision is made randomly and repeated for the next few invocations.
/// </summary>
abstract class FlakyObject
{
    private readonly Random _random;
    private readonly double _pFail;

    private const int _repeatCount = 5;

    private readonly object _lock = new();

    private int _repeatsRemaining;
    private bool _isRepeatingFailure;

    /// <param name="random">Randomness passed in for determinism purposes.</param>
    /// <param name="pFail">The probability of failure, a value in [0, 1].</param>
    public FlakyObject(Random random, double pFail)
    {
        Debug.Assert(pFail >= 0);
        Debug.Assert(pFail <= 1); // Allow 100% failure rate
        _pFail = pFail;
        _random = random;
    }

    public void MaybeFail()
    {
        if (_pFail <= 0)
        {
            return;
        }

        lock (_lock)
        {
            if (_repeatsRemaining == 0)
            {
                _isRepeatingFailure = _random.NextDouble() < _pFail;
                _repeatsRemaining = _repeatCount;
            }

            _repeatsRemaining--;

            if (_isRepeatingFailure)
            {
                throw new InvalidOperationException("Flakiness!");
            }
        }
    }
}

/// <summary>
/// Represents a backend like Azure Blob Storage that can store and retrieve XML elements.
/// Calls have a probability of failing, which is used to simulate network issues.
/// </summary>
sealed class FlakyXmlRepository(Random random, double pFail) : FlakyObject(random, pFail), IXmlRepository
{
    // No thread safety required
    private readonly List<XElement> _elements = new();
    private int _getAllElementsCallCount;
    private int _storeElementCallCount;

    public int GetAllElementsCallCount => _getAllElementsCallCount;
    public int StoreElementCallCount => _storeElementCallCount;

    IReadOnlyCollection<XElement> IXmlRepository.GetAllElements()
    {
        _getAllElementsCallCount++;
        // Simulate blob storage issues
        MaybeFail();
        return _elements.AsReadOnly();
    }

    void IXmlRepository.StoreElement(XElement element, string _friendlyName)
    {
        _storeElementCallCount++;
        // Simulate blob storage issues
        MaybeFail();
        _elements.Add(element);
    }
}

/// <summary>
/// Represents a backend like Azure Key Vault that can store and retrieve XML elements.
/// Calls have a probability of failing, which is used to simulate network issues.
/// </summary>
sealed class FlakyXmlEncryptor(Random random, double pFail) : FlakyObject(random, pFail), IXmlEncryptor
{
    private int _encryptCallCount;

    public int EncryptCallCount => _encryptCallCount;

    EncryptedXmlInfo IXmlEncryptor.Encrypt(XElement plaintextElement)
    {
        _encryptCallCount++;
        // Simulate AKV issues
        MaybeFail();
        return new EncryptedXmlInfo(plaintextElement, typeof(IXmlDecryptor)); // Activator will know what to do
    }
}

/// <summary>
/// Represents a backend like Azure Key Vault that can store and retrieve XML elements.
/// Calls have a probability of failing, which is used to simulate network issues.
/// </summary>
sealed class FlakyXmlDecryptor(Random random, double pFail) : FlakyObject(random, pFail), IXmlDecryptor
{
    private int _decryptCallCount;

    public int DecryptCallCount => _decryptCallCount;

    XElement IXmlDecryptor.Decrypt(XElement encryptedElement)
    {
        _decryptCallCount++;
        // Simulate AKV issues
        MaybeFail();
        return encryptedElement;
    }
}
