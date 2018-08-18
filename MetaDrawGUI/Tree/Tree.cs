﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaDrawGUI
{
    public class Tree
    {
        

        private Node root;
        public Tree(Node node)
        {
            root = node;
        }
    }

    public class Node
    {
        internal Char value;
        internal Node lChild;
        internal Node rChild;
        internal Node father;

        public Node(Char v, Node l, Node r)
        {
            value = v;
            lChild = l;
            rChild = r;
        }

        public Node(Char v)
        {
            value = v;
            lChild = null;
            rChild = null;
        }
    }
}
