// 關係圖表互動邏輯服務
// 處理所有與 D3.js 相關的圖表操作、拖曳、縮放等互動邏輯

import { Injectable, ElementRef } from '@angular/core';
import * as d3 from 'd3';
import { GraphData, GraphNode, GraphLink } from '../../../services/relationship-graph.service';
import { GraphStateService } from './graph-state.service';

@Injectable({
  providedIn: 'root'
})
export class GraphInteractionService {
  private svg: any;
  private simulation: any;
  private transform: any;
  private hiddenNodes: Set<string> = new Set();

  constructor(private stateService: GraphStateService) {}

  // === 初始化方法 ===

  initializeGraph(
    containerElement: ElementRef,
    graphData: GraphData,
    onNodeClick: (event: MouseEvent, node: GraphNode) => void,
    onNodeContextMenu: (event: MouseEvent, node: GraphNode) => void
  ): void {
    this.clearGraph();
    this.createSvg(containerElement);
    this.setupSimulation(graphData);
    this.renderGraph(graphData, onNodeClick, onNodeContextMenu);
  }

  private clearGraph(): void {
    if (this.svg) {
      this.svg.remove();
    }
    if (this.simulation) {
      this.simulation.stop();
    }
  }

  private createSvg(containerElement: ElementRef): void {
    const container = d3.select(containerElement.nativeElement);
    const rect = containerElement.nativeElement.getBoundingClientRect();
    
    this.svg = container
      .append('svg')
      .attr('width', '100%')
      .attr('height', '100%')
      .attr('viewBox', `0 0 ${rect.width} ${rect.height}`)
      .call(d3.zoom<SVGSVGElement, unknown>()
        .scaleExtent([0.1, 4])
        .on('zoom', (event) => {
          this.transform = event.transform;
          this.svg.select('.graph-group').attr('transform', this.transform);
        }) as any);

    // 添加定義區域（用於漸變和陰影效果）
    const defs = this.svg.append('defs');
    
    // 創建節點漸變
    const gradient = defs.append('linearGradient')
      .attr('id', 'nodeGradient')
      .attr('x1', '0%')
      .attr('y1', '0%')
      .attr('x2', '100%')
      .attr('y2', '100%');
    
    gradient.append('stop')
      .attr('offset', '0%')
      .attr('stop-color', 'rgba(255, 255, 255, 0.8)')
      .attr('stop-opacity', 0.8);
    
    gradient.append('stop')
      .attr('offset', '100%')
      .attr('stop-color', 'rgba(255, 255, 255, 0.1)')
      .attr('stop-opacity', 0.1);

    // 創建主要繪圖群組
    this.svg.append('g').attr('class', 'graph-group');
  }

  private setupSimulation(graphData: GraphData): void {
    const rect = this.svg.node().getBoundingClientRect();
    
    this.simulation = d3.forceSimulation(graphData.nodes)
      .force('link', d3.forceLink(graphData.links)
        .id((d: any) => d.id)
        .distance(100)
        .strength(0.1))
      .force('charge', d3.forceManyBody()
        .strength(-300)
        .distanceMax(400))
      .force('center', d3.forceCenter(rect.width / 2, rect.height / 2))
      .force('collision', d3.forceCollide().radius(40));
  }

  // === 圖表渲染方法 ===

  private renderGraph(
    graphData: GraphData,
    onNodeClick: (event: MouseEvent, node: GraphNode) => void,
    onNodeContextMenu: (event: MouseEvent, node: GraphNode) => void
  ): void {
    const graphGroup = this.svg.select('.graph-group');

    // 渲染連線
    this.renderLinks(graphGroup, graphData.links);
    
    // 渲染節點
    this.renderNodes(graphGroup, graphData.nodes, onNodeClick, onNodeContextMenu);

    // 啟動模擬
    this.simulation.on('tick', () => {
      this.updatePositions();
    });
  }

  private renderLinks(graphGroup: any, links: GraphLink[]): void {
    const linkGroup = graphGroup.append('g').attr('class', 'links');
    
    linkGroup.selectAll('line')
      .data(links)
      .enter().append('line')
      .attr('class', 'link')
      .attr('stroke', (d: GraphLink) => this.getLinkColor(d.type))
      .attr('stroke-width', 2)
      .attr('stroke-opacity', 0.7);
  }

  private renderNodes(
    graphGroup: any,
    nodes: GraphNode[],
    onNodeClick: (event: MouseEvent, node: GraphNode) => void,
    onNodeContextMenu: (event: MouseEvent, node: GraphNode) => void
  ): void {
    const nodeGroup = graphGroup.append('g').attr('class', 'nodes');
    
    const nodeElements = nodeGroup.selectAll('g')
      .data(nodes)
      .enter().append('g')
      .attr('class', 'node')
      .attr('data-node-id', (d: GraphNode) => d.id)
      .call(this.createDragBehavior())
      .on('click', (event: MouseEvent, d: GraphNode) => {
        event.stopPropagation();
        onNodeClick(event, d);
      })
      .on('contextmenu', (event: MouseEvent, d: GraphNode) => {
        event.preventDefault();
        event.stopPropagation();
        onNodeContextMenu(event, d);
      });

    // 添加節點陰影
    nodeElements.append('circle')
      .attr('r', 26)
      .attr('fill', 'rgba(0, 0, 0, 0.1)')
      .attr('transform', 'translate(2, 2)');

    // 添加節點主圓圈
    nodeElements.append('circle')
      .attr('r', 25)
      .attr('fill', (d: GraphNode) => this.getNodeColor(d))
      .attr('stroke', '#fff')
      .attr('stroke-width', 3)
      .style('filter', 'drop-shadow(0 2px 4px rgba(0, 0, 0, 0.2))')
      .style('cursor', 'pointer');

    // 添加漸變光澤效果
    nodeElements.append('circle')
      .attr('r', 22)
      .attr('fill', 'url(#nodeGradient)')
      .attr('opacity', 0.3)
      .style('pointer-events', 'none');

    // 添加節點內文字（性別或初始字母）
    nodeElements.append('text')
      .attr('text-anchor', 'middle')
      .attr('dy', '.35em')
      .attr('font-size', '14px')
      .attr('font-weight', 'bold')
      .attr('fill', 'white')
      .style('text-shadow', '0 1px 2px rgba(0, 0, 0, 0.5)')
      .style('pointer-events', 'none')
      .text((d: GraphNode) => this.getNodeText(d));

    // 添加姓名標籤
    nodeElements.append('text')
      .attr('text-anchor', 'middle')
      .attr('dy', '3.2em')
      .attr('font-size', '11px')
      .attr('font-weight', '600')
      .attr('fill', '#333')
      .style('text-shadow', '0 1px 2px rgba(255, 255, 255, 0.8)')
      .style('pointer-events', 'none')
      .text((d: GraphNode) => d.name || '未知');
  }

  private updatePositions(): void {
    // 更新連線位置
    this.svg.selectAll('.link')
      .attr('x1', (d: any) => d.source.x)
      .attr('y1', (d: any) => d.source.y)
      .attr('x2', (d: any) => d.target.x)
      .attr('y2', (d: any) => d.target.y);

    // 更新節點位置
    this.svg.selectAll('.node')
      .attr('transform', (d: any) => `translate(${d.x},${d.y})`);
  }

  // === 拖曳行為 ===

  private createDragBehavior(): any {
    return d3.drag()
      .on('start', (event: any, d: any) => {
        const state = this.stateService.getCurrentState();
        if (!state.interactionState.isDragging) return;
        
        if (!event.active) this.simulation.alphaTarget(0.3).restart();
        d.fx = d.x;
        d.fy = d.y;
        
        this.stateService.updateInteractionState({ isDraggingNode: true });
      })
      .on('drag', (event: any, d: any) => {
        const state = this.stateService.getCurrentState();
        if (!state.interactionState.isDragging) return;
        
        d.fx = event.x;
        d.fy = event.y;
      })
      .on('end', (event: any, d: any) => {
        if (!event.active) this.simulation.alphaTarget(0);
        d.fx = null;
        d.fy = null;
        
        this.stateService.updateInteractionState({ isDraggingNode: false });
      });
  }

  // === 縮放控制方法 ===

  zoomIn(): void {
    if (!this.svg) return;
    
    this.svg.transition().duration(300).call(
      this.svg.__zoom.scaleBy,
      1.5
    );
  }

  zoomOut(): void {
    if (!this.svg) return;
    
    this.svg.transition().duration(300).call(
      this.svg.__zoom.scaleBy,
      1 / 1.5
    );
  }

  resetView(): void {
    if (!this.svg) return;
    
    this.svg.transition().duration(500).call(
      this.svg.__zoom.transform,
      d3.zoomIdentity
    );
  }

  // === 節點高亮方法 ===

  highlightNode(nodeId: string): void {
    this.svg.selectAll('.node')
      .style('opacity', (d: any) => d.id === nodeId ? 1 : 0.3);
    
    this.svg.selectAll('.link')
      .style('opacity', (d: any) => 
        d.source.id === nodeId || d.target.id === nodeId ? 0.8 : 0.1
      );
  }

  clearHighlight(): void {
    this.svg.selectAll('.node').style('opacity', 1);
    this.svg.selectAll('.link').style('opacity', 0.7);
  }

  // === 節點狀態更新方法 ===

  updateNodeStates(): void {
    // 確保 SVG 已經初始化
    if (!this.svg) {
      return;
    }
    
    const state = this.stateService.getCurrentState();
    
    this.svg.selectAll('.node')
      .classed('selected', false)
      .classed('selected-first', false)
      .classed('selected-second', false)
      .classed('merge-first', false)
      .classed('merge-second', false);

    // 關係建立模式的高亮
    if (state.graphMode === 'create-relationship') {
      if (state.selectionState.firstSelectedNode) {
        this.svg.select(`[data-node-id="${state.selectionState.firstSelectedNode.id}"]`)
          .classed('selected-first', true);
      }
      if (state.selectionState.secondSelectedNode) {
        this.svg.select(`[data-node-id="${state.selectionState.secondSelectedNode.id}"]`)
          .classed('selected-second', true);
      }
    }

    // 合併模式的高亮
    if (state.graphMode === 'merge') {
      if (state.selectionState.firstSelectedNodeForMerge) {
        this.svg.select(`[data-node-id="${state.selectionState.firstSelectedNodeForMerge.id}"]`)
          .classed('merge-first', true);
      }
      if (state.selectionState.secondSelectedNodeForMerge) {
        this.svg.select(`[data-node-id="${state.selectionState.secondSelectedNodeForMerge.id}"]`)
          .classed('merge-second', true);
      }
    }
  }

  // === 新關係添加方法 ===

  addNewRelationship(sourceId: string, targetId: string, relationshipType: string): void {
    if (!this.svg || !this.simulation) return;

    // 創建新的連線資料
    const newLink = {
      id: `${sourceId}-${targetId}`,
      source: sourceId,
      target: targetId,
      type: 'family', // 或根據 relationshipType 決定
      label: relationshipType
    };

    // 獲取當前連線資料
    const currentLinks = this.simulation.force('link').links();
    const newLinks = [...currentLinks, newLink];

    // 更新力模擬
    this.simulation.force('link')
      .links(newLinks)
      .id((d: any) => d.id);

    // 重新渲染連線
    const linkGroup = this.svg.select('.links');
    const linkSelection = linkGroup.selectAll('line')
      .data(newLinks, (d: any) => d.id);

    linkSelection.enter().append('line')
      .attr('class', 'link')
      .attr('stroke', (d: any) => this.getLinkColor(d.type))
      .attr('stroke-width', 2)
      .attr('stroke-opacity', 0.7);

    // 重新啟動模擬
    this.simulation.alpha(0.3).restart();
  }

  // === 輔助方法 ===

  private getNodeColor(node: GraphNode): string {
    // 根據性別返回顏色
    const gender = node.gender?.toLowerCase();
    if (gender === 'm' || gender === 'male') {
      return '#42A5F5'; // 藍色代表男性
    } else if (gender === 'f' || gender === 'female') {
      return '#F48FB1'; // 粉色代表女性
    }
    return '#9E9E9E'; // 灰色代表未知
  }

  private getLinkColor(linkType: string): string {
    switch (linkType) {
      case 'family':
        return '#ff6b35'; // 橙色代表家族關係
      case 'friend':
        return '#666666'; // 灰色代表朋友關係
      default:
        return '#999999'; // 預設灰色
    }
  }

  private getNodeText(node: GraphNode): string {
    // 顯示姓名的第一個字或縮寫
    if (node.name) {
      return node.name.charAt(0).toUpperCase();
    }
    return '?';
  }

  // === 清理方法 ===

  destroy(): void {
    if (this.simulation) {
      this.simulation.stop();
    }
    if (this.svg) {
      this.svg.remove();
    }
    this.hiddenNodes.clear();
  }

  // === 節點過濾方法 ===

  filterNodes(searchTerm: string): void {
    if (!searchTerm.trim()) {
      this.showAllNodes();
      return;
    }

    this.svg.selectAll('.node')
      .style('display', (d: any) => {
        const isMatch = d.name?.toLowerCase().includes(searchTerm.toLowerCase()) ||
                       d.id?.toString().includes(searchTerm);
        return isMatch ? 'block' : 'none';
      });
  }

  private showAllNodes(): void {
    this.svg.selectAll('.node').style('display', 'block');
  }

  // === 獲取圖表資訊 ===

  getGraphBounds(): { width: number; height: number } {
    if (!this.svg) return { width: 0, height: 0 };
    
    const rect = this.svg.node().getBoundingClientRect();
    return { width: rect.width, height: rect.height };
  }

  getCurrentTransform(): any {
    return this.transform;
  }
}