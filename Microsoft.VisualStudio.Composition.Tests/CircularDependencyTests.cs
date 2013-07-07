﻿namespace Microsoft.VisualStudio.Composition.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Composition;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;

    public class CircularDependencyTests
    {
        #region Tight loop of all shared exports

        [MefFact(CompositionEngines.V2, typeof(SharedExport1), typeof(SharedExport2))]
        public void CircularDependenciesSharedExports(IContainer container)
        {
            var export1 = container.GetExport<SharedExport1>();
            var export2 = container.GetExport<SharedExport2>();
            Assert.Same(export1.Export2, export2);
            Assert.Same(export2.Export1, export1);
        }

        [Export, Shared]
        public class SharedExport1
        {
            [Import]
            public SharedExport2 Export2 { get; set; }
        }

        [Export, Shared]
        public class SharedExport2
        {
            [Import]
            public SharedExport1 Export1 { get; set; }
        }

        #endregion

        #region Loop with just one shared export

        [MefFact(CompositionEngines.V2, typeof(SharedExportInLoopOfNonShared), typeof(NonSharedExportInLoopWithShared), typeof(AnotherNonSharedExportInLoopWithShared))]
        public void CircularDependenciesOneSharedExport(IContainer container)
        {
            var shared = container.GetExport<SharedExportInLoopOfNonShared>();
            var nonShared = container.GetExport<NonSharedExportInLoopWithShared>();
            var anotherNonShared = container.GetExport<AnotherNonSharedExportInLoopWithShared>();
            Assert.NotSame(nonShared, shared.Other);
            Assert.NotSame(anotherNonShared, nonShared.Another);
            Assert.NotSame(anotherNonShared, shared.Other.Another);
            Assert.Same(shared, anotherNonShared.Shared);
        }

        [Export, Shared]
        public class SharedExportInLoopOfNonShared
        {
            [Import]
            public NonSharedExportInLoopWithShared Other { get; set; }
        }

        [Export]
        public class NonSharedExportInLoopWithShared
        {
            [Import]
            public AnotherNonSharedExportInLoopWithShared Another { get; set; }
        }

        [Export]
        public class AnotherNonSharedExportInLoopWithShared
        {
            [Import]
            public SharedExportInLoopOfNonShared Shared { get; set; }
        }

        #endregion

        #region Large loop of all non-shared exports

        [Fact]
        public void LoopOfNonSharedExports()
        {
            // There is no way to resolve this catalog. It would instantiate parts forever.
            Assert.Throws<InvalidOperationException>(() => CompositionConfiguration.Create(
                typeof(NonSharedPart1),
                typeof(NonSharedPart2),
                typeof(NonSharedPart3)));
        }

        [Export]
        public class NonSharedPart1
        {
            [Import]
            public NonSharedPart2 Part2 { get; set; }
        }

        [Export]
        public class NonSharedPart2
        {
            [Import]
            public NonSharedPart3 Part3 { get; set; }
        }

        [Export]
        public class NonSharedPart3
        {
            [Import]
            public NonSharedPart1 Part1 { get; set; }
        }

        #endregion

        #region Imports self

        [Fact(Skip = "StackOverflows")]
        public void SelfImportingNonSharedPartThrows()
        {
            Assert.Throws<AggregateException>(() => TestUtilities.CreateContainer(typeof(SelfImportingNonSharedPart)));
        }

        [MefFact(CompositionEngines.V2, typeof(SelfImportingSharedPart))]
        public void SelfImportingSharedPartSucceeds(IContainer container)
        {
            var value = container.GetExport<SelfImportingSharedPart>();
            Assert.Same(value, value.Self);
        }

        [Export]
        public class SelfImportingNonSharedPart
        {
            [Import]
            public SelfImportingNonSharedPart Self { get; set; }
        }

        [Export, Shared]
        public class SelfImportingSharedPart
        {
            [Import]
            public SelfImportingSharedPart Self { get; set; }
        }

        #endregion
    }
}