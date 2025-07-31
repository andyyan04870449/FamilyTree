import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { OrgNode, OrgConnection } from '../../services/organization-chart.service';

interface Line {
  x1: number;
  y1: number;
  x2: number;
  y2: number;
  type: 'solid' | 'dashed';
}

@Component({
  selector: 'app-org-connections',
  standalone: true,
  imports: [CommonModule],
  template: `
    <svg class="connections-svg">
      <defs>
        <marker
          id="arrowhead-down"
          markerWidth="10"
          markerHeight="10"
          refX="5"
          refY="0"
          orient="0"
        >
          <polygon
            points="0 0, 10 0, 5 10"
            fill="var(--primary)"
            opacity="0.8"
          />
        </marker>
        <marker
          id="arrowhead-down-dashed"
          markerWidth="10"
          markerHeight="10"
          refX="5"
          refY="0"
          orient="0"
        >
          <polygon
            points="0 0, 10 0, 5 10"
            fill="var(--text-tertiary)"
            opacity="0.6"
          />
        </marker>
      </defs>
      <g class="connections-group">
        <path
          *ngFor="let line of lines"
          [attr.d]="getPathData(line)"
          [attr.stroke-dasharray]="line.type === 'dashed' ? '5,5' : ''"
          class="connection-line"
          [class.dashed]="line.type === 'dashed'"
          [attr.marker-end]="line.type === 'dashed' ? 'url(#arrowhead-down-dashed)' : 'url(#arrowhead-down)'"
        />
      </g>
    </svg>
  `,
  styles: [`
    :host {
      position: absolute;
      top: 0;
      left: 0;
      width: 100%;
      height: 100%;
      pointer-events: none;
      z-index: 0; /* 確保在節點下方 */
    }
    
    .connections-svg {
      width: 100%;
      height: 100%;
    }
    
    .connection-line {
      stroke: var(--primary);
      stroke-width: 2;
      fill: none;
      opacity: 0.8;
      transition: all 0.3s ease;
      
      &.dashed {
        stroke: var(--text-tertiary);
        opacity: 0.6;
      }
      
      &:hover {
        opacity: 1;
        stroke-width: 3;
        filter: drop-shadow(0 0 3px var(--primary));
      }
    }
  `]
})
export class OrgConnectionsComponent implements OnChanges {
  @Input() connections: OrgConnection[] = [];
  @Input() nodes: Record<string, OrgNode> = {};
  
  lines: Line[] = [];
  
  ngOnChanges(changes: SimpleChanges): void {
    if (changes['connections'] || changes['nodes']) {
      this.calculateLines();
    }
  }
  
  private calculateLines(): void {
    this.lines = this.connections
      .map(connection => {
        const fromNode = this.nodes[connection.from];
        const toNode = this.nodes[connection.to];
        
        if (!fromNode || !toNode) return null;
        
        // 節點尺寸（根據實際節點設計調整）
        const nodeWidth = 260;
        const nodeHeight = 90; // padding 16px * 2 + avatar 50px + 一些邊距
        
        // 計算節點水平中心點（用於保持線條置中）
        const fromCenterX = fromNode.x + nodeWidth / 2;
        const toCenterX = toNode.x + nodeWidth / 2;
        
        // 判斷相對位置，決定從哪個邊連接
        const fromY = fromNode.y;
        const toY = toNode.y;
        
        let x1, y1, x2, y2;
        
        // 起點和終點都在水平中心
        x1 = fromCenterX;
        x2 = toCenterX;
        
        // 根據相對位置決定連接點
        if (fromY < toY) {
          // fromNode 在 toNode 上方，從下邊連到上邊
          y1 = fromNode.y + nodeHeight;  // 從 fromNode 的下邊緣
          y2 = toNode.y - 10;             // 到 toNode 的上邊緣減去箭頭高度
        } else if (fromY > toY) {
          // fromNode 在 toNode 下方，從上邊連到下邊
          y1 = fromNode.y;                // 從 fromNode 的上邊緣
          y2 = toNode.y + nodeHeight - 10; // 到 toNode 的下邊緣減去箭頭高度
        } else {
          // 同一水平線上，預設從下邊連到上邊
          if (fromCenterX < toCenterX) {
            // fromNode 在左，toNode 在右
            y1 = fromNode.y + nodeHeight;
            y2 = toNode.y + nodeHeight - 10;
          } else {
            // fromNode 在右，toNode 在左
            y1 = fromNode.y;
            y2 = toNode.y - 10;
          }
        }
        
        return {
          x1,
          y1,
          x2,
          y2,
          type: connection.type || 'solid'
        };
      })
      .filter(line => line !== null) as Line[];
  }
  
  getPathData(line: Line): string {
    const { x1, y1, x2, y2 } = line;
    
    // 如果是垂直連接（x 座標相同或相近）
    if (Math.abs(x2 - x1) < 5) {
      // 直線連接
      return `M ${x1} ${y1} L ${x2} ${y2}`;
    }
    
    // 計算垂直距離
    const dy = y2 - y1;
    const dx = x2 - x1;
    
    // 如果是從上到下的連接（最常見的情況）
    if (dy > 0) {
      // 使用平滑的貝茲曲線，控制點在垂直方向上
      const controlOffset = Math.min(Math.abs(dy) * 0.5, 50);
      return `M ${x1} ${y1} C ${x1} ${y1 + controlOffset}, ${x2} ${y2 - controlOffset}, ${x2} ${y2}`;
    } 
    // 如果是從下到上的連接
    else if (dy < 0) {
      // 使用平滑的貝茲曲線
      const controlOffset = Math.min(Math.abs(dy) * 0.5, 50);
      return `M ${x1} ${y1} C ${x1} ${y1 - controlOffset}, ${x2} ${y2 + controlOffset}, ${x2} ${y2}`;
    } 
    // 如果是同一水平線
    else {
      // 使用拱形曲線
      const arcHeight = Math.min(Math.abs(dx) * 0.3, 40);
      const midX = (x1 + x2) / 2;
      // 根據方向決定拱形方向
      const arcY = dx > 0 ? y1 + arcHeight : y1 - arcHeight;
      return `M ${x1} ${y1} Q ${midX} ${arcY}, ${x2} ${y2}`;
    }
  }
}