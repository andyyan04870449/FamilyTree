import { TestBed } from '@angular/core/testing';
import { TimeUtilsService } from './time-utils.service';
import { TimeRangeFactory } from '../utils/time-range.factory';

describe('TimeUtilsService', () => {
  let service: TimeUtilsService;
  let timeRangeFactory: jasmine.SpyObj<TimeRangeFactory>;

  beforeEach(() => {
    const timeRangeFactorySpy = jasmine.createSpyObj('TimeRangeFactory', ['createCustomRange']);

    TestBed.configureTestingModule({
      providers: [
        { provide: TimeRangeFactory, useValue: timeRangeFactorySpy }
      ]
    });
    
    service = TestBed.inject(TimeUtilsService);
    timeRangeFactory = TestBed.inject(TimeRangeFactory) as jasmine.SpyObj<TimeRangeFactory>;
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('formatDateTime', () => {
    const testDate = new Date('2023-01-15T10:30:45.123Z');

    it('should format date with predefined formats', () => {
      const shortFormat = service.formatDateTime(testDate, 'short');
      expect(shortFormat).toBeTruthy();
      
      const mediumFormat = service.formatDateTime(testDate, 'medium');
      expect(mediumFormat).toBeTruthy();
      
      const longFormat = service.formatDateTime(testDate, 'long');
      expect(longFormat).toBeTruthy();
    });

    it('should format date with custom format options', () => {
      const customFormat = service.formatDateTime(testDate, {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit'
      });
      expect(customFormat).toBeTruthy();
    });

    it('should handle different input types', () => {
      const dateObject = service.formatDateTime(testDate, 'short');
      const dateString = service.formatDateTime(testDate.toISOString(), 'short');
      const timestamp = service.formatDateTime(testDate.getTime(), 'short');

      expect(dateObject).toBeTruthy();
      expect(dateString).toBeTruthy();
      expect(timestamp).toBeTruthy();
    });

    it('should handle invalid dates gracefully', () => {
      const result = service.formatDateTime('invalid-date', 'short');
      expect(result).toBe('');
    });

    it('should handle different locales', () => {
      const zhTW = service.formatDateTime(testDate, 'short', 'zh-TW');
      const enUS = service.formatDateTime(testDate, 'short', 'en-US');
      
      expect(zhTW).toBeTruthy();
      expect(enUS).toBeTruthy();
    });
  });

  describe('formatRelativeTime', () => {
    it('should format past times correctly', () => {
      const now = new Date();
      const oneHourAgo = new Date(now.getTime() - 60 * 60 * 1000);
      const oneDayAgo = new Date(now.getTime() - 24 * 60 * 60 * 1000);
      
      const hourResult = service.formatRelativeTime(oneHourAgo);
      const dayResult = service.formatRelativeTime(oneDayAgo);
      
      expect(hourResult).toContain('小時前');
      expect(dayResult).toContain('天前');
    });

    it('should format future times correctly', () => {
      const now = new Date();
      const oneHourLater = new Date(now.getTime() + 60 * 60 * 1000);
      
      const result = service.formatRelativeTime(oneHourLater);
      expect(result).toContain('小時後');
    });

    it('should handle very recent times', () => {
      const now = new Date();
      const justNow = new Date(now.getTime() - 30 * 1000); // 30 seconds ago
      
      const result = service.formatRelativeTime(justNow);
      expect(result).toBe('剛剛');
    });
  });

  describe('getDateDifference', () => {
    it('should calculate date differences correctly', () => {
      const start = new Date('2023-01-01T00:00:00');
      const end = new Date('2023-01-02T12:30:45');
      
      const diff = service.getDateDifference(start, end);
      
      expect(diff.days).toBe(1);
      expect(diff.hours).toBeGreaterThan(24);
      expect(diff.minutes).toBeGreaterThan(1440);
    });

    it('should handle same dates', () => {
      const date = new Date('2023-01-01T12:00:00');
      const diff = service.getDateDifference(date, date);
      
      expect(diff.milliseconds).toBe(0);
      expect(diff.days).toBe(0);
    });

    it('should handle reversed date order', () => {
      const early = new Date('2023-01-01');
      const late = new Date('2023-01-02');
      
      const diff1 = service.getDateDifference(early, late);
      const diff2 = service.getDateDifference(late, early);
      
      expect(diff1.days).toBe(diff2.days);
    });

    it('should throw error for invalid dates', () => {
      expect(() => service.getDateDifference('invalid', new Date())).toThrow();
    });
  });

  describe('isDateInRange', () => {
    it('should correctly identify dates in range', () => {
      const start = new Date('2023-01-01');
      const end = new Date('2023-01-31');
      const inRange = new Date('2023-01-15');
      const outOfRange = new Date('2023-02-15');
      
      expect(service.isDateInRange(inRange, start, end)).toBeTruthy();
      expect(service.isDateInRange(outOfRange, start, end)).toBeFalsy();
    });

    it('should handle boundary dates', () => {
      const start = new Date('2023-01-01');
      const end = new Date('2023-01-31');
      
      expect(service.isDateInRange(start, start, end)).toBeTruthy();
      expect(service.isDateInRange(end, start, end)).toBeTruthy();
    });

    it('should handle invalid dates gracefully', () => {
      const result = service.isDateInRange('invalid', new Date(), new Date());
      expect(result).toBeFalsy();
    });
  });

  describe('getDayBounds', () => {
    it('should get correct day boundaries', () => {
      const testDate = new Date('2023-01-15T14:30:45');
      const bounds = service.getDayBounds(testDate);
      
      expect(bounds.start.getHours()).toBe(0);
      expect(bounds.start.getMinutes()).toBe(0);
      expect(bounds.start.getSeconds()).toBe(0);
      expect(bounds.start.getMilliseconds()).toBe(0);
      
      expect(bounds.end.getHours()).toBe(23);
      expect(bounds.end.getMinutes()).toBe(59);
      expect(bounds.end.getSeconds()).toBe(59);
      expect(bounds.end.getMilliseconds()).toBe(999);
    });

    it('should throw error for invalid date', () => {
      expect(() => service.getDayBounds('invalid')).toThrow();
    });
  });

  describe('isWorkingDay', () => {
    it('should identify working days correctly', () => {
      const monday = new Date('2023-01-16'); // Monday
      const tuesday = new Date('2023-01-17'); // Tuesday
      const saturday = new Date('2023-01-21'); // Saturday
      const sunday = new Date('2023-01-22'); // Sunday
      
      expect(service.isWorkingDay(monday)).toBeTruthy();
      expect(service.isWorkingDay(tuesday)).toBeTruthy();
      expect(service.isWorkingDay(saturday)).toBeFalsy();
      expect(service.isWorkingDay(sunday)).toBeFalsy();
    });

    it('should handle invalid dates', () => {
      expect(service.isWorkingDay('invalid')).toBeFalsy();
    });
  });

  describe('getNextWorkingDay', () => {
    it('should get next working day correctly', () => {
      const friday = new Date('2023-01-20'); // Friday
      const nextWorkingDay = service.getNextWorkingDay(friday);
      
      expect(nextWorkingDay.getDay()).toBe(1); // Monday
    });

    it('should skip weekends', () => {
      const saturday = new Date('2023-01-21'); // Saturday
      const nextWorkingDay = service.getNextWorkingDay(saturday);
      
      expect(nextWorkingDay.getDay()).toBe(1); // Monday
    });

    it('should throw error for invalid date', () => {
      expect(() => service.getNextWorkingDay('invalid')).toThrow();
    });
  });

  describe('describeDateRange', () => {
    it('should describe same day range', () => {
      const date = new Date('2023-01-15');
      const description = service.describeDateRange(date, date);
      
      expect(description).toContain('同一天');
    });

    it('should describe multi-day ranges', () => {
      const start = new Date('2023-01-01');
      const end = new Date('2023-01-07');
      const description = service.describeDateRange(start, end);
      
      expect(description).toContain('天') || expect(description).toContain('週');
    });

    it('should describe month ranges', () => {
      const start = new Date('2023-01-01');
      const end = new Date('2023-03-01');
      const description = service.describeDateRange(start, end);
      
      expect(description).toContain('個月');
    });

    it('should handle invalid dates', () => {
      const description = service.describeDateRange('invalid', 'invalid');
      expect(description).toBe('無效的日期範圍');
    });
  });

  describe('parseDate', () => {
    it('should parse different date formats', () => {
      const isoDate = service.parseDate('2023-01-15T10:30:00Z');
      const simpleDate = service.parseDate('2023-01-15');
      const timestamp = service.parseDate(1673776200000);
      const dateObject = service.parseDate(new Date('2023-01-15'));
      
      expect(isoDate).toBeInstanceOf(Date);
      expect(simpleDate).toBeInstanceOf(Date);
      expect(timestamp).toBeInstanceOf(Date);
      expect(dateObject).toBeInstanceOf(Date);
    });

    it('should return null for invalid inputs', () => {
      expect(service.parseDate('')).toBeNull();
      expect(service.parseDate(null as any)).toBeNull();
      expect(service.parseDate('invalid-date')).toBeNull();
    });

    it('should handle edge cases', () => {
      expect(service.parseDate(0)).toBeInstanceOf(Date); // Unix epoch
      expect(service.parseDate('1970-01-01')).toBeInstanceOf(Date);
    });
  });

  describe('Time Zone Operations', () => {
    it('should provide available time zones', () => {
      const timeZones = service.getAvailableTimeZones();
      expect(timeZones.length).toBeGreaterThan(0);
      expect(timeZones.every(tz => tz.timeZone && tz.label && tz.offset)).toBeTruthy();
    });

    it('should get browser time zone', () => {
      const browserTz = service.getBrowserTimeZone();
      expect(browserTz).toBeTruthy();
    });
  });

  describe('Edge Cases and Error Handling', () => {
    it('should handle null and undefined gracefully', () => {
      expect(service.formatDateTime(null as any)).toBe('');
      expect(service.formatRelativeTime(undefined as any)).toBe('');
      expect(service.parseDate(null as any)).toBeNull();
    });

    it('should handle extreme dates', () => {
      const farFuture = new Date('2100-01-01');
      const farPast = new Date('1900-01-01');
      
      expect(service.formatDateTime(farFuture, 'short')).toBeTruthy();
      expect(service.formatDateTime(farPast, 'short')).toBeTruthy();
    });

    it('should handle format errors gracefully', () => {
      // Test with a potentially problematic date format
      const result = service.formatDateTime(new Date(), { timeZone: 'Invalid/TimeZone' } as any);
      expect(typeof result).toBe('string');
    });
  });
});