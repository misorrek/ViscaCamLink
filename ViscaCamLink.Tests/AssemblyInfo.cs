using Xunit;

// Several units under test touch process-wide static state — the localized-resource
// culture (Strings.Culture), the current thread culture, and the TranslationSource.Instance
// singleton. Running test classes in parallel lets one test mutate that state while another
// reads it, causing sporadic failures. Disabling parallelization keeps the suite deterministic.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
