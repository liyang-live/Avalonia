// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Moq;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class ControlTests
    {
        [Fact]
        public void Classes_Should_Initially_Be_Empty()
        {
            var target = new Control();

            Assert.Equal(0, target.Classes.Count);
        }

        [Fact]
        public void Setting_Parent_Should_Also_Set_InheritanceParent()
        {
            var parent = new Decorator();
            var target = new TestControl();

            parent.Child = target;

            Assert.Equal(parent, target.Parent);
            Assert.Equal(parent, target.InheritanceParent);
        }

        [Fact]
        public void Setting_Parent_Should_Not_Set_InheritanceParent_If_Already_Set()
        {
            var parent = new Decorator();
            var inheritanceParent = new Decorator();
            var target = new TestControl();

            ((ISetInheritanceParent)target).SetParent(inheritanceParent);
            parent.Child = target;

            Assert.Equal(parent, target.Parent);
            Assert.Equal(inheritanceParent, target.InheritanceParent);
        }

        [Fact]
        public void InheritanceParent_Should_Be_Cleared_When_Removed_From_Parent()
        {
            var parent = new Decorator();
            var target = new TestControl();

            parent.Child = target;
            parent.Child = null;

            Assert.Null(target.InheritanceParent);
        }

        [Fact]
        public void InheritanceParent_Should_Be_Cleared_When_Removed_From_Parent_When_Has_Different_InheritanceParent()
        {
            var parent = new Decorator();
            var inheritanceParent = new Decorator();
            var target = new TestControl();

            ((ISetInheritanceParent)target).SetParent(inheritanceParent);
            parent.Child = target;
            parent.Child = null;

            Assert.Null(target.InheritanceParent);
        }

        [Fact]
        public void AttachedToLogicalParent_Should_Be_Called_When_Added_To_Tree()
        {
            var root = new TestRoot();
            var parent = new Border();
            var child = new Border();
            var grandchild = new Border();
            var parentRaised = false;
            var childRaised = false;
            var grandchildRaised = false;

            parent.AttachedToLogicalTree += (s, e) => parentRaised = true;
            child.AttachedToLogicalTree += (s, e) => childRaised = true;
            grandchild.AttachedToLogicalTree += (s, e) => grandchildRaised = true;

            parent.Child = child;
            child.Child = grandchild;

            Assert.False(parentRaised);
            Assert.False(childRaised);
            Assert.False(grandchildRaised);

            root.Child = parent;

            Assert.True(parentRaised);
            Assert.True(childRaised);
            Assert.True(grandchildRaised);
        }

        [Fact]
        public void AttachedToLogicalParent_Should_Be_Called_Before_Parent_Change_Signalled()
        {
            var root = new TestRoot();
            var child = new Border();
            var raised = new List<string>();

            child.AttachedToLogicalTree += (s, e) =>
            {
                Assert.Equal(root, child.Parent);
                raised.Add("attached");
            };

            child.GetObservable(Control.ParentProperty).Skip(1).Subscribe(_ => raised.Add("parent"));

            root.Child = child;

            Assert.Equal(new[] { "attached", "parent" }, raised);
        }

        [Fact]
        public void DetachedToLogicalParent_Should_Be_Called_When_Removed_From_Tree()
        {
            var root = new TestRoot();
            var parent = new Border();
            var child = new Border();
            var grandchild = new Border();
            var parentRaised = false;
            var childRaised = false;
            var grandchildRaised = false;

            parent.Child = child;
            child.Child = grandchild;
            root.Child = parent;

            parent.DetachedFromLogicalTree += (s, e) => parentRaised = true;
            child.DetachedFromLogicalTree += (s, e) => childRaised = true;
            grandchild.DetachedFromLogicalTree += (s, e) => grandchildRaised = true;

            root.Child = null;

            Assert.True(parentRaised);
            Assert.True(childRaised);
            Assert.True(grandchildRaised);
        }

        [Fact]
        public void Adding_Tree_To_IStyleRoot_Should_Style_Controls()
        {
            using (AvaloniaLocator.EnterScope())
            {
                var root = new TestRoot();
                var parent = new Border();
                var child = new Border();
                var grandchild = new Control();
                var styler = new Mock<IStyler>();

                AvaloniaLocator.CurrentMutable.Bind<IStyler>().ToConstant(styler.Object);

                parent.Child = child;
                child.Child = grandchild;

                styler.Verify(x => x.ApplyStyles(It.IsAny<IStyleable>()), Times.Never());

                root.Child = parent;

                styler.Verify(x => x.ApplyStyles(parent), Times.Once());
                styler.Verify(x => x.ApplyStyles(child), Times.Once());
                styler.Verify(x => x.ApplyStyles(grandchild), Times.Once());
            }
        }

        [Fact]
        public void Styles_Not_Applied_Until_Initialization_Finished()
        {
            using (AvaloniaLocator.EnterScope())
            {
                var root = new TestRoot();
                var child = new Border();
                var styler = new Mock<IStyler>();

                AvaloniaLocator.CurrentMutable.Bind<IStyler>().ToConstant(styler.Object);

                ((ISupportInitialize)child).BeginInit();
                root.Child = child;
                styler.Verify(x => x.ApplyStyles(It.IsAny<IStyleable>()), Times.Never());

                ((ISupportInitialize)child).EndInit();
                styler.Verify(x => x.ApplyStyles(child), Times.Once());
            }
        }

        [Fact]
        public void Adding_To_Logical_Tree_Should_Register_With_NameScope()
        {
            using (AvaloniaLocator.EnterScope())
            {
                var root = new TestRoot();
                var child = new Border();

                child.Name = "foo";
                root.Child = child;

                Assert.Same(root.FindControl<Border>("foo"), child);
            }
        }

        [Fact]
        public void Name_Cannot_Be_Set_After_Added_To_Logical_Tree()
        {
            using (AvaloniaLocator.EnterScope())
            {
                var root = new TestRoot();
                var child = new Border();

                root.Child = child;

                Assert.Throws<InvalidOperationException>(() => child.Name = "foo");
            }
        }

        [Fact]
        public void Name_Can_Be_Set_While_Initializing()
        {
            using (AvaloniaLocator.EnterScope())
            {
                var root = new TestRoot();
                var child = new Border();

                ((ISupportInitialize)child).BeginInit();
                root.Child = child;
                child.Name = "foo";
                Assert.Null(root.FindControl<Border>("foo"));
                ((ISupportInitialize)child).EndInit();

                Assert.Same(root.FindControl<Border>("foo"), child);
            }
        }

        [Fact]
        public void StyleDetach_Is_Triggered_When_Control_Removed_From_Logical_Tree()
        {
            using (AvaloniaLocator.EnterScope())
            {
                var root = new TestRoot();
                var child = new Border();

                root.Child = child;

                bool styleDetachTriggered = false;
                ((IStyleable)child).StyleDetach.Subscribe(_ => styleDetachTriggered = true);
                root.Child = null;

                Assert.True(styleDetachTriggered);
            }
        }

        [Fact]
        public void EndInit_Should_Raise_Initialized()
        {
            var root = new TestRoot();
            var target = new Border();
            var called = false;

            target.Initialized += (s, e) => called = true;
            ((ISupportInitialize)target).BeginInit();
            root.Child = target;
            ((ISupportInitialize)target).EndInit();

            Assert.True(called);
            Assert.True(target.IsInitialized);
        }

        [Fact]
        public void Attaching_To_Visual_Tree_Should_Raise_Initialized()
        {
            var root = new TestRoot();
            var target = new Border();
            var called = false;

            target.Initialized += (s, e) => called = true;
            root.Child = target;

            Assert.True(called);
            Assert.True(target.IsInitialized);
        }

        private class TestControl : Control
        {
            public new IAvaloniaObject InheritanceParent => base.InheritanceParent;
        }
    }
}
